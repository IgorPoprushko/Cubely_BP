using UnityEngine;
using UnityEngine.UI;

namespace Cubely.Blocks.Showcase
{
    /// <summary>A solid card with a genuine circular opening: no UI triangles are drawn inside it.</summary>
    public class CardWindowGraphic : MaskableGraphic
    {
        [SerializeField] private Vector2 holeCenter = new Vector2(0.5f, 0.65f);
        [SerializeField, Range(0.05f, 0.45f)] private float holeRadius = 0.34f;

        public Vector2 HoleCenter
        {
            get
            {
                var rect = rectTransform.rect;
                return rect.min + Vector2.Scale(rect.size, holeCenter);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = rectTransform.rect;
            var center = HoleCenter;
            float radius = Mathf.Min(rect.width * holeRadius, center.x - rect.xMin,
                rect.xMax - center.x, center.y - rect.yMin, rect.yMax - center.y);
            if (radius <= 0f) return;

            const int segmentsPerEdge = 16;
            const int segments = segmentsPerEdge * 4;
            for (int i = 0; i < segments; i++)
            {
                var outerA = Perimeter(rect, i, segmentsPerEdge);
                var outerB = Perimeter(rect, (i + 1) % segments, segmentsPerEdge);
                var innerA = center + (outerA - center).normalized * radius;
                var innerB = center + (outerB - center).normalized * radius;
                int start = vertices.currentVertCount;
                vertices.AddVert(outerA, color, Vector2.zero);
                vertices.AddVert(outerB, color, Vector2.zero);
                vertices.AddVert(innerB, color, Vector2.zero);
                vertices.AddVert(innerA, color, Vector2.zero);
                vertices.AddTriangle(start, start + 1, start + 2);
                vertices.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static Vector2 Perimeter(Rect rect, int index, int perEdge)
        {
            float t = (index % perEdge) / (float)perEdge;
            return (index / perEdge) switch
            {
                0 => new Vector2(rect.xMin, Mathf.Lerp(rect.yMin, rect.yMax, t)),
                1 => new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t), rect.yMax),
                2 => new Vector2(rect.xMax, Mathf.Lerp(rect.yMax, rect.yMin, t)),
                _ => new Vector2(Mathf.Lerp(rect.xMax, rect.xMin, t), rect.yMin),
            };
        }
    }
}
