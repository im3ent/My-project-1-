using UnityEngine;

// 1. 来源接口：挂在 ShopItem, InventoryItem, LootChest 上
public interface IItemContainer
{
    // 获取这个容器里的数据
    RuntimeItem GetRuntimeCard();

    // 询问是否允许开始拖拽 (例如：钱够不够？被冻结了吗？)
    bool CanDrag();

    // 当物品成功被别人拿走后，你需要做什么？
    // (商店：扣钱并销毁自己；背包：清除数据；宝箱：变空)
    void OnItemRemoved();
}

// 2. 目标接口：挂在 SellZone, InventorySlot, CharacterEquipment 上
public interface IDropTarget
{
    // 尝试接收物品
    // 返回 true = 接收成功 (交易完成)
    // 返回 false = 拒绝接收 (格子满了/类型不对)，物品会弹回原处
    bool OnItemDropped(RuntimeItem incomingItem);
}