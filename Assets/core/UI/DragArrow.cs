using System.Collections.Generic;
using UnityEngine;

namespace core.UI
{
    public class DragArrow : MonoBehaviour {
        // 单例，方便调用
        public static DragArrow Instance;

        [Header("外观设置")]
        public GameObject nodePrefab; // 小圆点
        public GameObject headPrefab; // 三角形箭头
        public int nodeCount = 10;    // 身体由多少个点组成？
        public float arcHeight = 2.0f; // 曲线拱起的高度

        [Header("内部变量")]
        private List<Transform> nodes = new List<Transform>();
        private Transform arrowHead;
        private bool isActive = false;
        
        void Awake() {
            Instance = this;
        }

        void Start() {
            // 1. 初始化对象池
            // 一开始就把所有点和箭头生成好，只是先隐藏起来
            // 这样不用每次拖拽都 Instantiate，性能更好
            for (int i = 0; i < nodeCount; i++) {
                GameObject node = Instantiate(nodePrefab, transform);
                node.SetActive(false);
                nodes.Add(node.transform);
            }

            GameObject head = Instantiate(headPrefab, transform);
            head.SetActive(false);
            arrowHead = head.transform;
        }

        // --- 核心方法：显示并更新箭头 ---
        // startPoint: 卡牌的位置
        // endPoint: 鼠标的位置
        public void UpdateArrow(Vector3 startPoint, Vector3 endPoint) {
            if (!isActive) Show();

            // 1. 计算控制点 (Control Point)
            // 贝塞尔曲线需要三个点：起点、终点、中间把线“吸”上去的控制点
            // 我们设控制点在：起点和终点的中点，再往上抬一点
            var midPoint = (startPoint + endPoint) / 2;
            var controlPoint = midPoint + Vector3.up * arcHeight;

            // 2. 分布小圆点
            for (var i = 0; i < nodeCount; i++) {
                // t 是一个 0 到 1 的比例 (0是起点，1是终点)
                var t = i / (float)nodeCount;

                // 调用贝塞尔公式计算坐标
                var position = CalculateBezierPoint(t, startPoint, controlPoint, endPoint);
            
                nodes[i].position = position;
                nodes[i].gameObject.SetActive(true);

                // 让圆点大小随距离变化（可选：越靠近目标越小）
                var scale = 1f - t * 0.5f; 
                nodes[i].localScale = Vector3.one * scale * 0.5f; // 0.5f 是基础大小
            }

            // 3. 设置箭头脑袋
            arrowHead.position = endPoint;
            arrowHead.gameObject.SetActive(true);

            // 4. 计算箭头旋转（让它指向鼠标移动的方向）
            // 获取最后一个圆点的位置，算出方向向量
            var direction = endPoint - nodes[nodeCount - 1].position;
            // 让箭头的“右边”指向目标 (2D Sprite 默认朝右)
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrowHead.rotation = Quaternion.Euler(0, 0, angle);
        }

        // --- 贝塞尔公式 ---
        // P0: 起点, P1: 控制点, P2: 终点, t: 0~1
        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2) {
            // 公式：(1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;

            var p = uu * p0; // 第一项
            p += 2 * u * t * p1; // 第二项
            p += tt * p2;        // 第三项
            return p;
        }

        public void Hide() {
            if (!isActive) return;
            isActive = false;

            foreach (var node in nodes) node.gameObject.SetActive(false);
            arrowHead.gameObject.SetActive(false);
        }

        void Show() {
            isActive = true;
        }


    }
}