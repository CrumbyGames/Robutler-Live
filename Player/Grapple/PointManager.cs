using Godot;
namespace Grapple
{
    // Loaded as singleton. Coded as generic as possible so that it can be reused for things other than grapple points.
    public class PointManager : Node
    {
        Point selectedPoint = null;

        // The point that is currently highlighted
        public Point SelectedPoint
        {
            get { return selectedPoint; }
            set
            {
                // Update points to know when they're selected.
                selectedPoint?.SetIsCurrent(false);
                selectedPoint = value;
                selectedPoint?.SetIsCurrent(true);
            }
        }

        // Connected to signals from points.
        public void _RequestForSelection(Point point) // Grapple Point passes itself for reference since singleton has no easy to access nodes
        {
            if (SelectedPoint != null)
            {
                // Only selects point if it has a higher priority than current
                if (point.SelectionPriority > SelectedPoint.SelectionPriority)
                {
                    SelectedPoint = point;
                }
            }
            else
            {
                SelectedPoint = point;
            }
        }

        // Connected to signals from points.
        public void _RequestForDeselection(Point point) // Grapple Point passes itself for reference since singleton has no easy to access nodes
        {
            if (point == SelectedPoint)
            {
                SelectedPoint = null;
            }
        }
    }
}