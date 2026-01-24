using UnityEngine;
using System.IO;
using SimpleJSON; // 如果报错找不到这个，说明第一步没做对
using cfg;        // 这是 Luban 生成的代码的命名空间

public class GameConfig : MonoBehaviour
{
    // 单例模式，方便全局调用
    public static Tables Tables;

    void Awake()
    {
        // 初始化加载器
        Tables = new Tables(LoadJson);
        
        Debug.Log("===> 配置表加载完毕！");
        
        // 【测试】打印第一张卡牌的名字（假设你的表名叫 TbCard）
        // 这里的 TbCard 需要换成你 Excel 里定义的实际表名
        // var card = Tables.TbCard.Get(1001); 
        // Debug.Log($"读取测试：{card.Name}"); 
    }

    // 这是一个委托函数，Luban 会自动调用它来读取文件
    private JSONNode LoadJson(string fileName)
    {
        // 拼凑文件路径：StreamingAssets/JsonData/文件名.json
        string filePath = Path.Combine(Application.streamingAssetsPath, "JsonData", fileName + ".json");
        
        if (File.Exists(filePath))
        {
            return JSON.Parse(File.ReadAllText(filePath));
        }
        else
        {
            Debug.LogError($"找不到配置文件：{filePath}");
            return null;
        }
    }
}