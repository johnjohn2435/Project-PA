using UnityEngine;

namespace PocketArcade.UI
{
    public static class HubParkNavigation
    {
        public static Vector2 Move(Vector2 position, Vector2 movement, Rect bounds, Rect[] obstacles)
        {
            // Small steps prevent crossing an entire building on a slow frame; axes allow wall sliding.
            int steps = Mathf.Max(1, Mathf.CeilToInt(movement.magnitude / .1f));
            Vector2 step = movement / steps;
            for (int i = 0; i < steps; i++)
            {
                var next = new Vector2(Mathf.Clamp(position.x + step.x, bounds.xMin, bounds.xMax), position.y);
                if (!Blocked(next, obstacles)) position = next;
                next = new Vector2(position.x, Mathf.Clamp(position.y + step.y, bounds.yMin, bounds.yMax));
                if (!Blocked(next, obstacles)) position = next;
            }
            return position;
        }
        private static bool Blocked(Vector2 position, Rect[] obstacles)
        {
            if (obstacles != null) foreach (var obstacle in obstacles) if (obstacle.Contains(position)) return true;
            return false;
        }
    }
}
