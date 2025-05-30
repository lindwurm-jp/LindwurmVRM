using Lindwurm.VRM;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class VRMSelector : MonoBehaviour
{
    [SerializeField] GameObject iconPrefab;

    private const string vrmPath = "../VRMData";
    private const string vrmFilter = "*.vrm";

    private readonly List<Sprite> cache = new();

    private void Start()
    {
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, vrmPath));
        LoadFiles(path);
    }

    private void LoadFiles(string path)
    {
        var dir = new DirectoryInfo(path);
        foreach (var file in dir.GetFiles(vrmFilter))
        {
            CreateIcon(file.FullName);
        }
    }

    private void CreateIcon(string path)
    {
        var icon = Instantiate(iconPrefab, transform);
        var vrm = new VRMData();
        _ = vrm.LoadMetaAsync(path, (meta) =>
        {
            if (icon.TryGetComponent<Image>(out var image))
            {
                var sprite = Sprite.Create(meta.thumbnail, new Rect(0, 0, meta.thumbnail.width, meta.thumbnail.height), Vector2.one * 0.5f);
                image.sprite = sprite;
                cache.Add(sprite);
            }
            if (icon.TryGetComponent<Button>(out var button))
            {
                button.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                    _ = LoadVRM(path);
                });
            }
        });
    }

    private async Task LoadVRM(string path)
    {
        var vrm = new VRMData();
        await vrm.LoadPathAsync(path);
    }

    private void OnDestroy()
    {
        foreach(var sprite in cache)
        {
            Destroy(sprite);
        }
        cache.Clear();
    }
}
