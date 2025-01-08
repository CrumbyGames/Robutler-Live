using Godot;
namespace Player
{
    public class @Camera : Camera2D
    {
        const float maxOffset = 150f;

        Vector2 centeredMousePosition; // The offset from the center of the camera to the mouse in coordinates, accounting for smoothing and drag margins.
        Vector2 relativeCenteredMousePosition; // A value between (-1, -1) and (1, 1) where (0, 0) is the centrer of the screen

        public Vector2 CenteredMousePosition
        {
            get { return centeredMousePosition; }
        }

        public Vector2 RelativeCenteredMousePosition
        {
            get { return centeredMousePosition; }
        }

        // Offset camera based on mouse position
        public override void _Input(InputEvent @event)
        {
            // Only updated when mouse is moving
            if (@event is InputEventMouseMotion mouseMotionEvent)
            {
                
                Vector2 containerSize = GetViewportRect().Size;
                centeredMousePosition = mouseMotionEvent.Position - containerSize / 2;
                relativeCenteredMousePosition = centeredMousePosition / (containerSize / 2f);
                relativeCenteredMousePosition = new Vector2(relativeCenteredMousePosition.x.Clamp(-1f, 1f), relativeCenteredMousePosition.y.Clamp(-1f, 1f));
                Position = Position.LinearInterpolate(relativeCenteredMousePosition * maxOffset, 0.1f);
            }
        }
    }
}
