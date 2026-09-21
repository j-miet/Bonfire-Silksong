using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace BonfireSilksong.GUIComponents
{
    /// <summary>
    /// Draws a disc visual i.e. circle edge + inner disc fill. Currently used for Challenge Taunt skill to display its 
    /// effect range.
    /// </summary>
    public class RangeIndicator
    {
        public string Name => _name;

        public RangeIndicator(string name)
        {
            _name = name;
        }

        public static void Add(RangeIndicator indicator)
        {
            _rangeIndicators.Add(indicator.Name, indicator);
        }

        /// <summary>
        /// Attach to SceneManager.SceneLoaded: this should properly hide the indicator on scene transitions.
        /// </summary>
        public static void OnSceneLoaded(Scene oldScene, LoadSceneMode mode)
        {
            foreach (var ind in _rangeIndicators.Values)
            {
                if (ind._rangeCircle != null && ind._rangeCircle.enabled)
                {
                    ind._rangeDisc.enabled = false;
                    ind._rangeCircle.enabled = false;
                }
            }
        }

        /// <summary>
        /// Add fading effect to range indicator by reducing alpha based on time left. Needs to be called in OnGUI.
        /// </summary>
        public static void UpdateFading()
        {
            foreach (var ind in _rangeIndicators.Values)
            {
                if (ind._rangeCircle == null || !ind._rangeCircle.enabled)
                    return;

                float remaining = ind._rangeIndicatorUntil - Time.time;

                if (remaining <= 0f)
                {
                    ind._rangeCircle.enabled = false;
                    return;
                }

                float alpha = remaining / RangeDisplayTime;

                // the circle has more alpha to clearly show the outer edge.
                Color circleColor = new Color(ind._circleColor.r, ind._circleColor.g, ind._circleColor.b, alpha);
                Color discColor = new Color(ind._discColor.r, ind._discColor.g, ind._discColor.b, alpha * 0.2f);

                ind._rangeCircle.startColor = circleColor;
                ind._rangeCircle.endColor = circleColor;
                ind._rangeDisc.material.color = discColor;
            }
        }

        /// <summary>
        /// Display a disc texture around the player. Use with UpdateFading.
        /// </summary>
        public static void Display(string indicatorName, float radius, Color? circleColor, Color? discColor)
        {
            var indicator = _rangeIndicators.TryGetValue(indicatorName, out var ind) ? ind : null;
            indicator?.Enable(radius, circleColor, discColor);
        }


        private const float RangeDisplayTime = 0.75f;

        private readonly string _name;
        public static Dictionary<string, RangeIndicator> _rangeIndicators = new Dictionary<string, RangeIndicator>();

        private LineRenderer _rangeCircle;
        private float _rangeIndicatorUntil;
        private Color _circleColor;

        private MeshRenderer _rangeDisc;
        private MeshFilter _rangeDiscMesh;
        private Color _discColor;

        private void Enable(float radius, Color? circleColor, Color? discColor)
        {
            _circleColor = circleColor ?? new Color(1f, 1f, 1f);
            _discColor = discColor ?? new Color(1f, 1f, 1f);

            if (_rangeCircle == null)
            {
                CreateRangeDisc(radius);
            }
            else
            {
                SetDiscRadius(radius);
            }

            _rangeCircle.enabled = true;
            _rangeDisc.enabled = true; // needed only because OnSceneLoaded disables this
            _rangeIndicatorUntil = Time.time + RangeDisplayTime;
        }

        private void CreateRangeDisc(float radius)
        {
            // first create the outer edge i.e. the circle
            GameObject circleObj = new GameObject("RangeCircle");
            circleObj.transform.SetParent(HeroController.instance.transform, false);

            _rangeCircle = circleObj.AddComponent<LineRenderer>();

            int positionCount = 128;
            float width = 0.1f;

            _rangeCircle.useWorldSpace = false;
            _rangeCircle.loop = true;
            _rangeCircle.positionCount = positionCount;
            _rangeCircle.startWidth = _rangeCircle.endWidth = width;

            _rangeCircle.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.white
            };

            // after outer circle, create the inner disc
            GameObject discObj = new GameObject("RangeDisc");
            discObj.transform.SetParent(circleObj.transform, false);

            _rangeDiscMesh = discObj.AddComponent<MeshFilter>();
            _rangeDisc = discObj.AddComponent<MeshRenderer>();

            _rangeDisc.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 1f, 1f, 0.2f)
            };

            CreateDiscMesh(radius);

            SetDiscRadius(radius);
        }

        private void CreateDiscMesh(float radius)
        {
            int segments = 128;

            Vector3[] vertices = new Vector3[segments + 1]; // segments + disc center
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                float segment = (float)i / segments;
                float angle = segment * Mathf.PI * 2f;

                vertices[i + 1] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );
            }

            for (int i = 0; i < segments; i++)
            {
                // this yields i+1 except at the final index (segments-1) it gives 0, which next + 1 below uses to
                // wrap around and close the circle.
                int next = (i + 1) % segments;

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = next + 1;
            }

            Mesh mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateBounds();

            _rangeDiscMesh.mesh = mesh;
        }

        private void SetDiscRadius(float newRadius)
        {
            int positionCount = _rangeCircle.positionCount;

            for (int pos = 0; pos < positionCount; pos++)
            {
                float segment = (float)pos / positionCount;
                float angle = segment * Mathf.PI * 2f;

                _rangeCircle.SetPosition(
                    pos,
                    new Vector3(
                        Mathf.Cos(angle) * newRadius,
                        Mathf.Sin(angle) * newRadius,
                        0f
                    )
                );
            }

            CreateDiscMesh(newRadius);
        }
    }
}