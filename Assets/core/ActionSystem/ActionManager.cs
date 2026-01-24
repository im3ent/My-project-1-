using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动作管理器 - 负责管理和执行游戏动作队列
/// 类似 Slay the Spire 的 ActionManager
/// </summary>
public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance { get; private set; }
    
    // 动作队列
    private readonly Queue<GameAction> _actionQueue = new();
    
    // 当前正在执行的动作
    private GameAction _currentAction;
    
    // 是否正在处理队列
    private bool _isProcessing;
    
    // 用于在当前动作执行期间插入的动作（优先执行）
    private readonly Queue<GameAction> _priorityQueue = new();
    
    /// <summary>
    /// 队列是否为空
    /// </summary>
    public bool IsQueueEmpty => _actionQueue.Count == 0 && _priorityQueue.Count == 0 && _currentAction == null;
    
    /// <summary>
    /// 当前队列长度
    /// </summary>
    public int QueueLength => _actionQueue.Count + _priorityQueue.Count;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// 将动作添加到队列末尾（常规方式）
    /// </summary>
    public void AddToBottom(GameAction action)
    {
        if (action == null) return;
        
        _actionQueue.Enqueue(action);
        
        // 如果当前没有在处理队列，开始处理
        if (!_isProcessing)
        {
            StartCoroutine(ProcessQueue());
        }
    }
    
    /// <summary>
    /// 将动作添加到队列顶部（优先执行，用于触发效果）
    /// 例如：打出法术后触发"抽一张牌"
    /// </summary>
    public void AddToTop(GameAction action)
    {
        if (action == null) return;
        
        _priorityQueue.Enqueue(action);
        
        // 如果当前没有在处理队列，开始处理
        if (!_isProcessing)
        {
            StartCoroutine(ProcessQueue());
        }
    }
    
    /// <summary>
    /// 清空所有待处理的动作（用于战斗结束等情况）
    /// </summary>
    public void ClearAll()
    {
        _actionQueue.Clear();
        _priorityQueue.Clear();
        _currentAction = null;
    }
    
    /// <summary>
    /// 核心：处理动作队列的协程
    /// </summary>
    private IEnumerator ProcessQueue()
    {
        _isProcessing = true;
        
        while (_priorityQueue.Count > 0 || _actionQueue.Count > 0)
        {
            // 优先队列先处理（触发效果）
            if (_priorityQueue.Count > 0)
            {
                _currentAction = _priorityQueue.Dequeue();
            }
            else if (_actionQueue.Count > 0)
            {
                _currentAction = _actionQueue.Dequeue();
            }
            
            if (_currentAction != null)
            {
                // 执行动作
                yield return StartCoroutine(_currentAction.Execute());
                
                // 动作执行完毕
                _currentAction = null;
            }
        }
        
        _isProcessing = false;
        
        // 队列处理完毕后，刷新场面
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBoardChanged();
        }
    }
    
    /// <summary>
    /// 等待队列清空（用于需要同步等待的场景）
    /// </summary>
    public IEnumerator WaitForQueueEmpty()
    {
        while (!IsQueueEmpty)
        {
            yield return null;
        }
    }
}
