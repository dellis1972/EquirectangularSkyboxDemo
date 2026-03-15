using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EquirectangularSkyboxDemo;

/// <summary>
/// First-person camera controlled by keyboard (WASD) and mouse look.
/// Orientation is stored as yaw + pitch floats and rebuilt as a Quaternion
/// each frame to avoid gimbal lock.
/// </summary>
public class QuaternionCamera
{
    // ---- Configuration -------------------------------------------------
    public float MoveSpeed      { get; set; } = 5f;
    public float LookSensitivity{ get; set; } = 0.005f;
    public float FieldOfView    { get; set; } = MathHelper.ToRadians(60f);
    public float NearPlane      { get; set; } = 0.1f;
    public float FarPlane       { get; set; } = 1000f;

    // ---- State ---------------------------------------------------------
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>Horizontal rotation in radians.</summary>
    public float Yaw   { get; set; } = 0f;

    /// <summary>Vertical rotation in radians, clamped to ±89°.</summary>
    public float Pitch { get; set; } = 0f;

    // ---- Cached matrices -----------------------------------------------
    private Matrix _view;
    private Matrix _projection;
    private int    _screenWidth;
    private int    _screenHeight;

    public Matrix View       => _view;
    public Matrix Projection => _projection;

    public QuaternionCamera(int screenWidth, int screenHeight)
    {
        _screenWidth  = screenWidth;
        _screenHeight = screenHeight;
        RebuildMatrices();
    }

    public void OnWindowResize(int screenWidth, int screenHeight)
    {
        _screenWidth  = screenWidth;
        _screenHeight = screenHeight;
    }

    /// <summary>
    /// Updates the camera from keyboard and mouse input.
    /// </summary>
    /// <param name="gameTime">Current game time.</param>
    /// <param name="keyboard">Current keyboard state.</param>
    /// <param name="mouse">Current mouse state.</param>
    /// <param name="isActive">Whether the game window is active.</param>
    public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse, bool isActive)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (isActive)
        {
            // --- Mouse look ---
            int centerX = _screenWidth  / 2;
            int centerY = _screenHeight / 2;

            float deltaX = mouse.X - centerX;
            float deltaY = mouse.Y - centerY;

            Yaw   -= deltaX * LookSensitivity;
            Pitch -= deltaY * LookSensitivity;

            // Clamp pitch to avoid flipping
            float maxPitch = MathHelper.ToRadians(89f);
            Pitch = MathHelper.Clamp(Pitch, -maxPitch, maxPitch);

            // --- Keyboard movement ---
            Quaternion orientation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, orientation);
            Vector3 right   = Vector3.Transform( Vector3.UnitX, orientation);

            if (keyboard.IsKeyDown(Keys.W)) Position += forward * MoveSpeed * dt;
            if (keyboard.IsKeyDown(Keys.S)) Position -= forward * MoveSpeed * dt;
            if (keyboard.IsKeyDown(Keys.A)) Position -= right   * MoveSpeed * dt;
            if (keyboard.IsKeyDown(Keys.D)) Position += right   * MoveSpeed * dt;
        }

        RebuildMatrices();
    }

    private void RebuildMatrices()
    {
        Quaternion orientation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);

        // Build view matrix: position + orientation
        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, orientation);
        Vector3 up      = Vector3.Transform( Vector3.UnitY, orientation);
        _view = Matrix.CreateLookAt(Position, Position + forward, up);

        _projection = Matrix.CreatePerspectiveFieldOfView(
            FieldOfView,
            (float)_screenWidth / _screenHeight,
            NearPlane,
            FarPlane);
    }
}
