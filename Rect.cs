using System.Numerics;

namespace ConfigGenerator;

struct Rect
{
    private Vector2 _leftUp;
    private Vector2 _rightDown;

    public Rect()
    {
        _leftUp = Vector2.Zero;
        _rightDown = Vector2.Zero;
    }
    
    public Rect(Vector2 leftUp, Vector2 rightDown)
    {
        _leftUp = leftUp;
        _rightDown = rightDown;
    }
    
    public Rect(Vector2 pos, int width, int height)
    {
        _leftUp = pos;
        _rightDown = new Vector2(pos.X + width, pos.Y + height);
    }

    public bool Overlaps(Rect otherRect)
    {
        return !(_rightDown.X < otherRect._leftUp.X || _leftUp.X > otherRect._rightDown.X ||
                 _leftUp.Y > otherRect._rightDown.Y || _rightDown.Y < otherRect._leftUp.Y);
    }
}