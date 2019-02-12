using System;
using System.Runtime.CompilerServices;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using ConsoleApp1.Common;
using ConsoleApp1.Structs;
using Newtonsoft.Json.Serialization;

namespace ConsoleApp1.Logic
{
    public class Dan
    {
        public double distance;
        public Vector3 normal;

        public Dan(double distance, Vector3 normal)
        {
            this.distance = distance;
            this.normal = normal;
        }
    }

    public class CollideLogic
    {
        public static double Middle(double min, double max)
        {
            return min + (max - min) / 2.0;
        }
        
        public static bool has_penetration(Vector3 pos_a, Vector3 pos_b, double rad_a, double rad_b)
        {
            var delta_position = pos_b - pos_a;
            var distance = delta_position.magnitude;
            var penetration = rad_a + rad_b - distance;
            return penetration > 0;
        }
        
        public static bool collide_entities(Unit a, Unit b)
        {            
            
            var delta_position = b.position - a.position;
            var distance = delta_position.magnitude;
            var penetration = a.radius + b.radius - distance;
            if (penetration > 0)
            {
                var k_a = (1.0 / a.mass) / ((1.0 / a.mass) + (1.0 / b.mass));
                var k_b = (1.0 / b.mass) / ((1.0 / a.mass) + (1.0 / b.mass));
                var normal = delta_position.normalized;
                a.position -= normal * penetration * k_a;
                b.position += normal * penetration * k_b;
                var delta_velocity = Vector3.Dot(b.velocity - a.velocity, normal) - b.radiusChangeSpeed - a.radiusChangeSpeed;
                if (delta_velocity < 0)
                {
//                    var impulse = (1 + Constants.MAX_HIT_E) * delta_velocity * normal;
                    var impulse = (1 + Middle(Constants.MIN_HIT_E, Constants.MAX_HIT_E)) * delta_velocity * normal;
                    a.velocity += impulse * k_a;
                    b.velocity -= impulse * k_b;
                }

                return true;
            }

            return false;
        }
        
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Dan dan_to_plane_min(Vector3 point, Vector3 point_on_plane, Vector3 plane_normal, Dan dan)
        {
            var distance = Vector3.Dot(point - point_on_plane, plane_normal);
            if(distance < dan.distance)
                return new Dan(distance, plane_normal);
            return dan;
        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Dan dan_to_sphere_inner_min(Vector3 point, Vector3 sphere_center, double sphere_radius, Dan dan)
        {
            var vector = sphere_center - point;
            var mag = vector.magnitude;
            double distance = sphere_radius - mag;
            if(distance < dan.distance)
                return new Dan(sphere_radius - mag, vector / mag);
            return dan;
        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Dan dan_to_sphere_outer_min(Vector3 point, Vector3 sphere_center, Dan dan)
        {
            var vector = (point - sphere_center);
            var mag = vector.magnitude;
            double distance = mag - MyArena.goal_side_radius;
            if(distance < dan.distance)
                return new Dan(distance, vector / mag);
            
            return dan;
        }

        private static Vector3 v_sub = new Vector3(
            (MyArena.goal_width / 2) - MyArena.goal_top_radius,
            MyArena.goal_height - MyArena.goal_top_radius);
        
        public static Dan dan_to_arena_quarter(Vector3 point)
        {
            // Ceiling
            Dan dan = new Dan(MyArena.height - point.y, Vector3.down);
            
            // Ground
            if (dan.distance > point.y)
            {
                dan.distance = point.y;
                dan.normal = Vector3.up;
            }

            double on_x = MyArena.width / 2 - point.x;
            if(dan.distance > on_x)
            {
                dan.distance = on_x;
                dan.normal = Vector3.left;
            }

            double on_z = (MyArena.depth / 2) + MyArena.goal_depth - point.z;
            if(dan.distance > on_z)
            {
                dan.distance = on_z;
                dan.normal = Vector3.back;
            }
            
            // Side z
            var v = new Vector3(point.x, point.y) - v_sub;
            if ((point.x >= (MyArena.goal_width / 2) + MyArena.goal_side_radius)
                || (point.y >= MyArena.goal_height + MyArena.goal_side_radius)
                || (v.x > 0 && v.y > 0 && v.magnitude >= MyArena.goal_top_radius + MyArena.goal_side_radius))
                dan = dan_to_plane_min(point, new Vector3(0, 0, MyArena.depth / 2), new Vector3(0, 0, -1), dan);

            // Side x & ceiling (goal)
            if (point.z >= (MyArena.depth / 2) + MyArena.goal_side_radius)
            {
                // x
                dan = dan_to_plane_min(
                    point,
                    new Vector3(MyArena.goal_width / 2, 0, 0),
                    new Vector3(-1, 0, 0),
                    dan);
                // y
                dan = dan_to_plane_min(
                    point, 
                    new Vector3(0, MyArena.goal_height, 0), 
                    new Vector3(0, -1, 0),
                    dan);
            }

            // Goal back corners
            if (point.z > (MyArena.depth / 2) + MyArena.goal_depth - MyArena.bottom_radius)
            {
                dan = dan_to_sphere_inner_min(
                    point,
                    new Vector3(
                        Math.Clamp(
                            point.x,
                            MyArena.bottom_radius - (MyArena.goal_width / 2),
                            (MyArena.goal_width / 2) - MyArena.bottom_radius
                        ),
                        Math.Clamp(
                            point.y,
                            MyArena.bottom_radius,
                            MyArena.goal_height - MyArena.goal_top_radius
                        ),
                        (MyArena.depth / 2) + MyArena.goal_depth - MyArena.bottom_radius),
                    MyArena.bottom_radius,
                    dan);
            }

            // Corner
            if ((point.x > (MyArena.width / 2) - MyArena.corner_radius) &&
                (point.z > (MyArena.depth / 2) - MyArena.corner_radius))
                dan = dan_to_sphere_inner_min(
                    point,
                    new Vector3(
                        (MyArena.width / 2) - MyArena.corner_radius,
                        point.y,
                        (MyArena.depth / 2) - MyArena.corner_radius
                    ),
                    MyArena.corner_radius,
                    dan);

            // Goal outer corner
            if (point.z < (MyArena.depth / 2) + MyArena.goal_side_radius)
            {
                // Side x
                if (point.x < (MyArena.goal_width / 2) + MyArena.goal_side_radius)
                {
                    dan = dan_to_sphere_outer_min(
                        point,
                        new Vector3(
                            (MyArena.goal_width / 2) + MyArena.goal_side_radius,
                            point.y,
                            (MyArena.depth / 2) + MyArena.goal_side_radius
                        ),
                        dan);
                }

                // Ceiling
                if (point.y < MyArena.goal_height + MyArena.goal_side_radius)
                    dan = dan_to_sphere_outer_min(
                        point,
                        new Vector3(
                            point.x,
                            MyArena.goal_height + MyArena.goal_side_radius,
                            (MyArena.depth / 2) + MyArena.goal_side_radius
                        ),
                        dan);
                // Top corner
                var o = new Vector3(
                    (MyArena.goal_width / 2) - MyArena.goal_top_radius,
                    MyArena.goal_height - MyArena.goal_top_radius
                );
                v = new Vector3(point.x, point.y) - o;
                if (v.x > 0 && v.y > 0)
                {
                    o = o + v.normalized * (MyArena.goal_top_radius + MyArena.goal_side_radius);
                    dan = dan_to_sphere_outer_min(
                        point,
                        new Vector3(o.x, o.y, (MyArena.depth / 2) + MyArena.goal_side_radius),
                        dan);
                }
            }

            // Goal inside top corners
            if ((point.z > (MyArena.depth / 2) + MyArena.goal_side_radius) &&
                point.y > (MyArena.goal_height - MyArena.goal_top_radius))
            {
                // Side x
                if (point.x > (MyArena.goal_width / 2) - MyArena.goal_top_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            (MyArena.goal_width / 2) - MyArena.goal_top_radius,
                            MyArena.goal_height - MyArena.goal_top_radius,
                            point.z
                        ),
                        MyArena.goal_top_radius,
                        dan);
                    // Side z
                if (point.z > (MyArena.depth / 2) + MyArena.goal_depth - MyArena.goal_top_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            point.x,
                            MyArena.goal_height - MyArena.goal_top_radius,
                            (MyArena.depth / 2) + MyArena.goal_depth - MyArena.goal_top_radius
                        ), 
                        MyArena.goal_top_radius,
                        dan);
            }

            // Bottom corners
            if (point.y < MyArena.bottom_radius)
            {
                // Side x
                if (point.x > (MyArena.width / 2) - MyArena.bottom_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            (MyArena.width / 2) - MyArena.bottom_radius,
                            MyArena.bottom_radius,
                            point.z
                        ),
                        MyArena.bottom_radius,
                        dan);
                // Side z
                if (point.z > (MyArena.depth / 2) - MyArena.bottom_radius &&
                    point.x >= (MyArena.goal_width / 2) + MyArena.goal_side_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            point.x,
                            MyArena.bottom_radius,
                            (MyArena.depth / 2) - MyArena.bottom_radius
                        ),
                        MyArena.bottom_radius,
                        dan);
                // Side z (goal)
                if (point.z > (MyArena.depth / 2) + MyArena.goal_depth - MyArena.bottom_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            point.x,
                            MyArena.bottom_radius,
                            (MyArena.depth / 2) + MyArena.goal_depth - MyArena.bottom_radius
                        ),
                        MyArena.bottom_radius,
                        dan);
                // Goal outer corner
                var o = new Vector3(
                    (MyArena.goal_width / 2) + MyArena.goal_side_radius,
                    (MyArena.depth / 2) + MyArena.goal_side_radius
                );
                v = new Vector3(point.x, point.z) - o;
                if (v.x < 0 && v.y < 0 && v.magnitude < MyArena.goal_side_radius + MyArena.bottom_radius)
                {
                    o = o + v.normalized * (MyArena.goal_side_radius + MyArena.bottom_radius);
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(o.x, MyArena.bottom_radius, o.y),
                        MyArena.bottom_radius,
                        dan);
                }

                // Side x (goal)
                if (point.z >= (MyArena.depth / 2) + MyArena.goal_side_radius &&
                    point.x > (MyArena.goal_width / 2) - MyArena.bottom_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            (MyArena.goal_width / 2) - MyArena.bottom_radius,
                            MyArena.bottom_radius,
                            point.z
                        ),
                        MyArena.bottom_radius,
                        dan);
                // Corner
                if (point.x > (MyArena.width / 2) - MyArena.corner_radius &&
                    point.z > (MyArena.depth / 2) - MyArena.corner_radius)
                {
                    var corner_o = new Vector3(
                        (MyArena.width / 2) - MyArena.corner_radius,
                        (MyArena.depth / 2) - MyArena.corner_radius
                    );
                    var n = new Vector3(point.x, point.z) - corner_o;
                    var dist = n.magnitude;
                    if (dist > MyArena.corner_radius - MyArena.bottom_radius)
                    {
                        n = n / dist;
                        var o2 = corner_o + n * (MyArena.corner_radius - MyArena.bottom_radius);
                        dan = dan_to_sphere_inner_min(
                            point,
                            new Vector3(o2.x, MyArena.bottom_radius, o2.y),
                            MyArena.bottom_radius,
                            dan);
                    }
                }
            }

            // Ceiling corners
            if (point.y > MyArena.height - MyArena.top_radius)
            {
                // Side x
                if (point.x > (MyArena.width / 2) - MyArena.top_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            (MyArena.width / 2) - MyArena.top_radius,
                            MyArena.height - MyArena.top_radius,
                            point.z 
                        ),
                        MyArena.top_radius,
                        dan);
                // Side z
                if (point.z > (MyArena.depth / 2) - MyArena.top_radius)
                    dan = dan_to_sphere_inner_min(
                        point,
                        new Vector3(
                            point.x,
                            MyArena.height - MyArena.top_radius,
                            (MyArena.depth / 2) - MyArena.top_radius
                        ),
                        MyArena.top_radius,
                        dan);

                // Corner
                if (point.x > (MyArena.width / 2) - MyArena.corner_radius
                    && point.z > (MyArena.depth / 2) - MyArena.corner_radius)
                {
                    var corner_o = new Vector3(
                        (MyArena.width / 2) - MyArena.corner_radius,
                        (MyArena.depth / 2) - MyArena.corner_radius
                    );
                    var dv = new Vector3(point.x, point.z) - corner_o;
                    if (dv.magnitude > MyArena.corner_radius - MyArena.top_radius)
                    {
                        var n = dv.normalized;
                        var o2 = corner_o + n * (MyArena.corner_radius - MyArena.top_radius);
                        dan = dan_to_sphere_inner_min(
                            point,
                            new Vector3(o2.x, MyArena.height - MyArena.top_radius, o2.y),
                            MyArena.top_radius,
                            dan);
                    }
                }
            }

            return dan;
        }
        
        
        public static Dan dan_to_arena(Vector3 point)
        {
            var negate_x = point.x < 0;
            var negate_z = point.z < 0;
            if (negate_x)
                point.x = -point.x;
            if (negate_z)
                point.z = -point.z;
            var result = dan_to_arena_quarter(point);
            if (negate_x)
                result.normal.x = -result.normal.x;
            if (negate_z)
                result.normal.z = -result.normal.z;
            return result;
        }


        public static Vector3? collide_with_arena(double radius, double radius_change_speed, double arena_e, ref Vector3 position, ref Vector3 velocity)
        {
            var dan = dan_to_arena(position);
            var penetration = radius - dan.distance;
            if (penetration > 0)
            {
                position += penetration * dan.normal;
                var velocityInner = Vector3.Dot(velocity, dan.normal) - radius_change_speed;
                if (velocityInner < 0)
                {
                    velocity -= (1 + arena_e) * velocityInner * dan.normal;
                    return dan.normal;
                }
            }

            return null;
        }


        static void move(ref Vector3 position, ref Vector3 velocity)
        {
            velocity = Vector3.ClampMagnitude(velocity, Constants.MAX_ENTITY_SPEED);
            position += velocity * Constants.MICROTICK_DELTA_TIME;
            position.y -= Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME * Constants.MICROTICK_DELTA_TIME / 2;
            velocity.y -= Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME;
        }

        public static void update_for_jump(MyRobot robot, Control control)
        {
            for(int i = 0; i < 100; ++i)
                update_microtick_for_jump(robot, control);
        }

        public static void update_microtick_for_jump(MyRobot robot, Control control) {
            if (robot.touch)
            {
                var target_velocity = Vector3.ClampMagnitude(
                    control.TargetVelocity,
                    Constants.ROBOT_MAX_GROUND_SPEED);
                target_velocity -= robot.touchNormal
                                   * Vector3.Dot(robot.touchNormal, target_velocity);
                var target_velocity_change = target_velocity - robot.velocity;
                if(target_velocity_change.magnitude > 0)
                {
                    var acceleration = Constants.ROBOT_ACCELERATION * Math.Max(0, robot.touchNormal.y);
                    robot.velocity += Vector3.ClampMagnitude(
                        target_velocity_change.normalized * acceleration * Constants.MICROTICK_DELTA_TIME,
                        target_velocity_change.magnitude);
                }
            }
 
            if (control.Nitro) {
                
                
                var target_velocity_change = Vector3.ClampMagnitude(
                    control.TargetVelocity - robot.velocity,
                    robot.nitroAmount * Constants.NITRO_POINT_VELOCITY_CHANGE);
                if (target_velocity_change.magnitude > 0) {
                    var acceleration = target_velocity_change.normalized * Constants.ROBOT_NITRO_ACCELERATION;
                    var velocity_change = Vector3.ClampMagnitude(
                        acceleration * Constants.MICROTICK_DELTA_TIME,
                        target_velocity_change.magnitude);
                    robot.velocity += velocity_change;
                    robot.nitroAmount -= velocity_change.magnitude / Constants.NITRO_POINT_VELOCITY_CHANGE;
                }
            }
            
            move(ref robot.position, ref robot.velocity);

            if (robot.touch)
            {
                var radius = MoveCalculator.RadiusFromJumpSpeed(control.JumpSpeed);
                var radius_change_speed = control.JumpSpeed;
                
                var collision_normal = collide_with_arena(radius, radius_change_speed, Constants.ROBOT_ARENA_E,
                    ref robot.position, ref robot.velocity);
                if (collision_normal == null)
                    robot.touch = false;
                else
                {
                    robot.touch = true;
                    robot.touchNormal = collision_normal.Value;
                }
            }

        }
    }
}