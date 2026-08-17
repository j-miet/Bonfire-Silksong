using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Draws a circle indicator, currently used for Challenge Taunt skill to display its effect range.
    /// </summary>
    public class RangeIndicator
    {
        /// <summary>
        /// Display a circle texture around the player. Use with UpdateFading.
        /// </summary>
        public static void Display(float radius)
        {
            if (_rangeCircle == null)
            {
                CreateRangeCircle(radius);
            }
            else
            {
                SetCircleRadius(radius);
            }

            _rangeCircle.enabled = true;
            _rangeIndicatorUntil = Time.time + RangeDisplayTime;
        }


        /// <summary>
        /// Add fading effect to range indicator by reducing alpha based on time left. Needs to be called in OnGUI.
        /// </summary>
        public static void UpdateFading()
        {
            if (_rangeCircle == null || !_rangeCircle.enabled)
                return;

            float remaining = _rangeIndicatorUntil - Time.time;

            if (remaining <= 0f)
            {
                _rangeCircle.enabled = false;
                return;
            }

            float alpha = remaining / RangeDisplayTime;

            Color color = new Color(1f, 1f, 1f, alpha);

            _rangeCircle.startColor = color;
            _rangeCircle.endColor = color;
        }


        private const float RangeDisplayTime = 0.75f;

        private static LineRenderer _rangeCircle;
        private static float _rangeIndicatorUntil;

        private static void CreateRangeCircle(float radius)
        {
            GameObject obj = new GameObject("TauntRangeIndicator");

            _rangeCircle = obj.AddComponent<LineRenderer>();

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

            SetCircleRadius(radius);

            // keep circle always around the player.
            obj.transform.SetParent(HeroController.instance.transform, false);
        }

        private static void SetCircleRadius(float newRadius)
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
        }
    }
}