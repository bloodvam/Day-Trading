using System.Windows.Forms;

namespace ChartEngine.Interfaces
{
    public interface IInputHandler
    {
        void OnMouseDown(MouseEventArgs e);
        void OnMouseMove(MouseEventArgs e);
        void OnMouseUp(MouseEventArgs e);
        void OnMouseWheel(MouseEventArgs e);
        void OnMouseLeave();
        void OnKeyDown(KeyEventArgs e);
        void OnKeyUp(KeyEventArgs e);
    }
}
