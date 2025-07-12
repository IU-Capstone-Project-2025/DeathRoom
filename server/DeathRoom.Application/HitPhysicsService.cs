using DeathRoom.Domain;

namespace DeathRoom.Application;

public class HitPhysicsService
{
    private const float CYLINDER_HEIGHT = 2.0f;
    private const float CYLINDER_RADIUS = 0.5f;

    public bool IsHit(Vector3 shooterPos, Vector3 shootDir, Vector3 targetPos)
    {
        // Нормализуем направление выстрела
        var normalizedDir = Normalize(shootDir);
        
        // Вектор от стрелка к цели
        var toTarget = targetPos - shooterPos;
        
        // Проверяем попадание в цилиндр
        return IsRayIntersectingCylinder(shooterPos, normalizedDir, targetPos, CYLINDER_RADIUS, CYLINDER_HEIGHT);
    }

    private bool IsRayIntersectingCylinder(Vector3 rayOrigin, Vector3 rayDirection, Vector3 cylinderCenter, float radius, float height)
    {
        // Смещаем цилиндр так, чтобы его центр был на уровне земли
        var cylinderBottom = cylinderCenter - new Vector3(0, height / 2, 0);
        var cylinderTop = cylinderCenter + new Vector3(0, height / 2, 0);

        // Проверяем пересечение луча с цилиндром
        var intersection = RayCylinderIntersection(rayOrigin, rayDirection, cylinderCenter, radius, height);
        
        return intersection.HasValue && intersection.Value > 0;
    }

    private float? RayCylinderIntersection(Vector3 rayOrigin, Vector3 rayDirection, Vector3 cylinderCenter, float radius, float height)
    {
        // Вектор от центра цилиндра к началу луча
        var oc = rayOrigin - cylinderCenter;
        
        // Проекция на XZ плоскость (игнорируем Y)
        var rayDirXZ = new Vector3(rayDirection.X, 0, rayDirection.Z);
        var ocXZ = new Vector3(oc.X, 0, oc.Z);
        
        // Квадрат длины проекции направления
        var a = rayDirXZ.X * rayDirXZ.X + rayDirXZ.Z * rayDirXZ.Z;
        var b = 2 * (rayDirXZ.X * ocXZ.X + rayDirXZ.Z * ocXZ.Z);
        var c = ocXZ.X * ocXZ.X + ocXZ.Z * ocXZ.Z - radius * radius;
        
        // Дискриминант
        var discriminant = b * b - 4 * a * c;
        
        if (discriminant < 0)
            return null; // Нет пересечения с цилиндром
            
        // Находим точки пересечения
        var t1 = (-b - MathF.Sqrt(discriminant)) / (2 * a);
        var t2 = (-b + MathF.Sqrt(discriminant)) / (2 * a);
        
        // Проверяем, что пересечение происходит в пределах высоты цилиндра
        var cylinderBottom = cylinderCenter.Y - height / 2;
        var cylinderTop = cylinderCenter.Y + height / 2;
        
        // Проверяем t1
        if (t1 > 0)
        {
            var intersectionPoint1 = rayOrigin + Multiply(rayDirection, t1);
            if (intersectionPoint1.Y >= cylinderBottom && intersectionPoint1.Y <= cylinderTop)
                return t1;
        }
        
        // Проверяем t2
        if (t2 > 0)
        {
            var intersectionPoint2 = rayOrigin + Multiply(rayDirection, t2);
            if (intersectionPoint2.Y >= cylinderBottom && intersectionPoint2.Y <= cylinderTop)
                return t2;
        }
        
        return null;
    }

    private Vector3 Normalize(Vector3 vector)
    {
        var length = MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        if (length == 0) return vector;
        return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
    }

    private Vector3 Multiply(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }
} 