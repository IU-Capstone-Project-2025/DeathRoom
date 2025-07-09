using DeathRoom.Domain;

namespace DeathRoom.Application;

public class HitPhysicsService
{
    private const float CYLINDER_RELATIVE_HEIGHT = 2;
    private readonly Vector3 CYLINDER_SHIFT = new Vector3(0, 0, CYLINDER_RELATIVE_HEIGHT);

    private float AngleCos(Vector3 first, Vector3 second)
        => (first * second) / (!first * !second);

    public bool IsHit(Vector3 shooterPos, Vector3 shootDir, Vector3 targetPos)
    {
        // Преобразование вектора в XZ-плоскости
        var shootProj = shootDir.projection(ProjectionCode.xz);
        var radius = targetPos - shooterPos;
        var radProj = radius.projection(ProjectionCode.xz);
        bool projectionHits = !radProj / MathF.Sqrt(!radProj * !radProj - 1) <= AngleCos(radProj, shootProj);
        if (!projectionHits) return false;
        // Проверка нижней сферы
        var botRadius = radius - CYLINDER_SHIFT;
        if (!botRadius / MathF.Sqrt(!botRadius * !botRadius - 1) <= AngleCos(botRadius, shootDir))
            return true;
        // Проверка верхней сферы
        var topRadius = radius + CYLINDER_SHIFT;
        if (!topRadius / MathF.Sqrt(!topRadius * !topRadius - 1) <= AngleCos(topRadius, shootDir))
            return true;
        // Проверка цилиндра
        float yIntersection = shootDir.Y * !radProj / (radius.X * shootDir.X + radius.Z * shootDir.Z);
        if (radius.Y - CYLINDER_RELATIVE_HEIGHT <= yIntersection && yIntersection <= radius.Y + CYLINDER_RELATIVE_HEIGHT)
            return true;
        // Если ничего не сработало — промах
        return false;
    }
} 