using UnityEngine;
using UniVRM10;

namespace Lindwurm.VRM
{
	public class VRMSpringBoneCenter : MonoBehaviour
	{
		[SerializeField] float moveLimit = 5f;

		private Vector3 prevPosition;

		public static VRMSpringBoneCenter Create(Transform parent)
		{
			var obj = new GameObject("SpringBoneCenter");
			obj.transform.SetParent(parent);
			var center = obj.AddComponent<VRMSpringBoneCenter>();
			var vrm = obj.GetComponentInParent<Vrm10Instance>();
			if (!vrm)
			{
				Debug.LogError($"No VRM10Instance in {parent.name}");
				return null;
			}
			foreach (var spring in vrm.SpringBone.Springs)
			{
				spring.Center = obj.transform;
			}
			vrm.Runtime.SpringBone.ReconstructSpringBone();
			center.ResetPosition();
			return center;
		}

		public void ResetPosition()
		{
			transform.localPosition = Vector3.zero;
			prevPosition = transform.parent.position;
		}

		private void Update()
		{
			var dt = Time.deltaTime;
			var limit = moveLimit * dt;
			var dv = prevPosition - transform.parent.position;
			transform.localPosition = dv.sqrMagnitude > limit * limit ? dv.normalized * limit : dv;
			prevPosition = transform.parent.position;
		}
	}
}
