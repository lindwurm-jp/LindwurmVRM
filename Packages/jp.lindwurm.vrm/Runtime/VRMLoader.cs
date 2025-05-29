using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using UniGLTF.Extensions.VRMC_vrm;
using UnityEngine;
using UniVRM10;

namespace Lindwurm.VRM
{
	public class VRMData : System.IDisposable
    {
        [System.Serializable]
        public class Meta
        {
            public Texture2D thumbnail;
            public string name;
            public string authors;
            public AvatarPermissionType avatarPermission;
            public bool? violentUsage;
            public bool? sexualUsage;
            public CommercialUsageType commercialUsage;
        }

        public class MetaHolder : MonoBehaviour
        {
            [SerializeField] Meta meta;
            public Meta Meta => meta;
            public void SetData(Meta meta) => this.meta = meta;
        }

		public class SpringBone
		{
			private readonly IVrm10SpringBoneRuntime springBone;

			public SpringBone(IVrm10SpringBoneRuntime springBone) { this.springBone = springBone; }
			public void ReconstructSpringBone() => springBone.ReconstructSpringBone();
			public void RestoreInitialTransform() => springBone.RestoreInitialTransform();
		}

        public enum Status
        {
            Loading,
            LoadCompleted,
            LoadFailed,
            LoadCanceled,
        }

        public Status status { get; private set; }
        public Vrm10Instance instance { get; private set; }
		public GameObject gameObject => instance ? instance.gameObject : null;
		public SpringBone springBone { get; private set; }

		private readonly Meta meta = new();
        private readonly CancellationTokenSource cts;
        private System.Action<Meta> metaInformationCallback = null;
		private bool disposedValue;

		public VRMData() { cts = new CancellationTokenSource(); }
        public VRMData(CancellationToken ct) { cts = CancellationTokenSource.CreateLinkedTokenSource(ct); }

        private void Initialize(Vrm10Instance instance)
        {
            this.instance = instance;
			springBone = new SpringBone(instance.Runtime.SpringBone);
			instance.gameObject.AddComponent<MetaHolder>().SetData(meta);
        }

		/// <summary>
		/// ロード済みVRMを再利用する
		/// </summary>
		/// <param name="obj"></param>
        public void SetLoadedVRMInstance(GameObject obj)
        {
            if (obj && obj.TryGetComponent<Vrm10Instance>(out var instance))
            {
                Initialize(instance);
                status = Status.LoadCompleted;
            }
            else
            {
                status = Status.LoadFailed;
            }
        }

        /// <summary>
        /// モデル表示
        /// </summary>
        /// <param name="isOn"></param>
        public void ShowMeshes(bool isOn)
        {
            if (instance && instance.gameObject)
            {
                var renderers = instance.gameObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                    renderer.enabled = isOn;
            }
        }

        /// <summary>
        /// メタデータを取得する
        /// </summary>
        /// <param name="path"></param>
        /// <param name="metaInformationCallback"></param>
        /// <returns></returns>
        public async Task LoadMetaAsync(string path, System.Action<Meta> metaInformationCallback)
        {
            this.metaInformationCallback = metaInformationCallback;
            var awaitCaller = new RuntimeOnlyAwaitCaller();
            using var gltfData = await awaitCaller.Run(() =>
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return new GlbLowLevelParser(path, bytes).Parse();
            });
            var vrm10 = Vrm10Data.Parse(gltfData);
            if (vrm10 != null)
            {
                using var loader = new Vrm10Importer(vrm10);
                var thumbnail = await loader.LoadVrmThumbnailAsync();
                SetMetaInfomation(thumbnail, vrm10.VrmExtension.Meta, null);
            }
            else
            {
                using var migratedData = Vrm10Data.Migrate(gltfData, out vrm10, out var migrationData);
                if (migratedData == null)
                {
                    throw new System.Exception(migrationData?.Message ?? "Failed to migrate.");
                }
                using var loader = new Vrm10Importer(vrm10);
                var thumbnail = await loader.LoadVrmThumbnailAsync();
                SetMetaInfomation(thumbnail, null, migrationData.OriginalMetaBeforeMigration);
            }
        }

		/// <summary>
		/// VRMファイル読み込み
		/// </summary>
		/// <param name="path"></param>
		/// <param name="showMeshes"></param>
		/// <param name="metaInformationCallback"></param>
		/// <returns></returns>
		public async Task LoadPathAsync(string path, bool showMeshes = true, System.Action<Meta> metaInformationCallback = null)
        {
            try
            {
                status = Status.Loading;
                this.metaInformationCallback = metaInformationCallback;
                var vrm10 = await Vrm10.LoadPathAsync(
					path,
					canLoadVrm0X: true,
					controlRigGenerationOption: ControlRigGenerationOption.None,
					showMeshes: showMeshes,
					awaitCaller: null,
					materialGenerator: new UrpVrm10MaterialDescriptorGenerator(),
					vrmMetaInformationCallback: SetMetaInfomation,
					ct: cts.Token);
                if (vrm10)
                {
                    status = Status.LoadCompleted;
                    Initialize(vrm10);
                }
                else
                {
                    status = Status.LoadFailed;
                    Debug.LogWarning($"VRM model load failed: {path}");
                }
            }
            catch (System.OperationCanceledException)
            {
                status = Status.LoadCanceled;
            }
            catch (System.Exception e)
            {
                status = Status.LoadFailed;
                Debug.LogWarning($"VRM model load exception: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// VRoidSDK用 VRMバイナリー読み込み
        /// </summary>
        /// <param name="characterBinary"></param>
        /// <param name="showMeshes"></param>
        /// <param name="metaInformationCallback"></param>
        /// <returns></returns>
        public async Task LoadVRMFromBinaryAsync(byte[] characterBinary, bool showMeshes = true, System.Action<Meta> metaInformationCallback = null)
        {
            try
            {
                status = Status.Loading;
                this.metaInformationCallback = metaInformationCallback;
				var vrm10 = await Vrm10.LoadBytesAsync(
					characterBinary,
					canLoadVrm0X: true,
					controlRigGenerationOption: ControlRigGenerationOption.None,
					showMeshes: showMeshes,
					awaitCaller: null,
					materialGenerator: new UrpVrm10MaterialDescriptorGenerator(),
					vrmMetaInformationCallback: SetMetaInfomation,
					ct: cts.Token);
				if (vrm10)
                {
                    status = Status.LoadCompleted;
                    Initialize(vrm10);
                }
                else
                {
                    status = Status.LoadFailed;
                    Debug.LogWarning($"VRM model load failed");
                }
            }
            catch (System.OperationCanceledException)
            {
                status = Status.LoadCanceled;
            }
            catch (System.Exception e)
            {
                status = Status.LoadFailed;
                Debug.LogWarning($"VRM model load exception: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// UniVRM10用 VrmMetaInformationCallback
        /// </summary>
        /// <param name="thumbnail"></param>
        /// <param name="meta"></param>
        /// <param name="meta0"></param>
        private void SetMetaInfomation(
            Texture2D thumbnail,
            UniGLTF.Extensions.VRMC_vrm.Meta meta,
            UniVRM10.Migration.Vrm0Meta meta0)
        {
            this.meta.thumbnail = thumbnail;
            if (meta != null)
            {
                this.meta.name = meta.Name;
                this.meta.authors = string.Join('/', meta.Authors);
                this.meta.avatarPermission = meta.AvatarPermission;
                this.meta.violentUsage = meta.AllowExcessivelyViolentUsage;
                this.meta.sexualUsage = meta.AllowExcessivelySexualUsage;
                this.meta.commercialUsage = meta.CommercialUsage;
            }
            else if (meta0 != null)
            {
                this.meta.name = meta0.title;
                this.meta.authors = meta0.author;
                this.meta.avatarPermission = (AvatarPermissionType)meta0.allowedUser;
                this.meta.violentUsage = meta0.violentUsage;
                this.meta.sexualUsage = meta0.sexualUsage;
                this.meta.commercialUsage = meta0.commercialUsage ? CommercialUsageType.corporation : CommercialUsageType.personalNonProfit;
            }
            metaInformationCallback?.Invoke(this.meta);
        }

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (status == Status.Loading)
						cts.Cancel();
					cts.Dispose();
					if (instance)
						Object.Destroy(instance.gameObject);
					instance = null;
				}
				springBone = null;
				metaInformationCallback = null;
				disposedValue = true;
			}
		}

		~VRMData()
		{
		    Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}
	}
}