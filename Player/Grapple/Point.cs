using Godot;

namespace Grapple
{
    public class Point : Node2D
    {
        const float MinSelectionPriority = 0.7f; // If the SelectionPriority is above this and outperforms other points, it gets selected.

        bool suppressOutline = false;
        bool current = false;

        Panel outline;
        PointManager manager;
        Player.Camera camera;

        [Signal]
        public delegate void RequestForDeselection(Point sourcePoint);

        [Signal]
        public delegate void RequestForSelection(Point sourcePoint);

        // Subject to change but currently calculated by using the dot product to determine how similar of an angle there is between the camera-point vector and the camera-mouse vector. (1 if identical, 0 if perpendicular, -1 if opposite)
        public float SelectionPriority
        {
            get
            {
                Vector2 vectorToCamera = GlobalPosition - camera.GlobalPosition;
                return vectorToCamera.Normalized().Dot(camera.CenteredMousePosition.Normalized());
            }
        }

        // As well as updating state, update outline if necessary
        public void SetIsCurrent(bool value)
        {
            current = value;
            if (!suppressOutline)
            {
                outline.Visible = value;
            }
        }

        // Force hide outline (currently used when point is actively being grappled)
        public void SetSuppressOutline(bool value)
        {
            suppressOutline = value;
            if (value)
            {
                outline.Visible = false;
            }
            else
            {
                // Manually update outline
                SetIsCurrent(current);
            }
        }

        public override void _Ready()
        {
            outline = GetNode<Panel>("Outline");
            camera = GetNode<Player.Camera>("%Camera");
            manager = GetNode<PointManager>("/root/PointManager");

            // Connect to singleton to emit signals instead of calling methods on it directly
            Connect("RequestForSelection", manager, "_RequestForSelection");
            Connect("RequestForDeselection", manager, "_RequestForDeselection");
        }

        public override void _Process(float delta)
        {
            // Keep checking whether point is suitable to be highlighted
            if (SelectionPriority > MinSelectionPriority)
            {
                EmitSignal("RequestForSelection", this);
            }
            else
            {
                EmitSignal("RequestForDeselection", this);
            }
        }

        // Forcefully deselect if point is off-screen.
        public void _OnScreenExited()
        {
            EmitSignal("RequestForDeselection", this);
        }
    }
}