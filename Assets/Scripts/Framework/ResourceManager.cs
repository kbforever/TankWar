using UnityEngine.AddressableAssets;
using UnityEngine;
using System.Threading.Tasks;

public static class ResourceManager
{

    public static T LoadResource<T>(string path) where T : Object
    {
        var obj = Resources.Load<T>(path) as T;
        if (obj == null)
        {
            Debug.Log($"this obj not in {path}");
        }
        return obj;
    }

    public static T AsycnLoadResource<T>(string path) where T : Object
    {
        var obj = Resources.LoadAsync<T>(path) as T;
        if (obj == null)
        {
            Debug.Log($"this obj not in {path}");
        }
        return obj;

    }


    public async static Task<T> AsycnLoadAddressable<T>(string name) where T:Object
    {
        
        T obj = await Addressables.LoadAssetAsync<T>(name).Task;
        return obj;
        
    }

    
}
