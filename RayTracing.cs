using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace CornishRoom
{
    class RayTracing
    {
        private static double eps = 1e-10;
        private int width, height;
        Point3D eye; //точка с которой смотрим
        private List<Light> lights;
        private List<Figure> scene;

        
        public RayTracing(int width, int height, List<Figure> _scene, List<Light> _lights, Point3D eye) 
        {
            lights = _lights;
            scene = _scene;
            this.width = width;
            this.height = height;
            this.eye = eye;
        }

        public Bitmap GetImage()
        {
            Bitmap newImg = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    Point3D point = Convert2DTo3D(i, j, width, height);
                    newImg.SetPixel(i, j, RayTrace(eye, point, 10)); 
                }
            return newImg;
        }

        
        private Color RayTrace(Point3D eye, Point3D ray, int iter)
        {
            (Figure nearest, double lenToNearest) = NearestElem(eye, ray);

            if (nearest == null)
                return Color.Black;

            // Вычисление пересечения
            Point3D pointIntersec = eye + lenToNearest * ray;

            Point3D normal;
            if (nearest.Type == FigureType.Sphere)
            {
                Sphere sphere = nearest as Sphere;
                normal = Normalize(pointIntersec - sphere.Center); //Вычисление нормали сферы в точке пересечения
            }
            else
            {
                Wall wall = nearest as Wall;
                normal = wall.Normal;
            }
            
            Color ambColor = nearest.ColorAmbient;
            double intens = CalcIntensity(pointIntersec, normal);
            int rColor = Math.Min((int)(ambColor.R * intens), 255);
            int gColor = Math.Min((int)(ambColor.G * intens), 255);
            int bColor = Math.Min((int)(ambColor.B * intens), 255);

            Color color = Color.FromArgb(rColor, gColor, bColor);

            // Если макс глубина / объект матовый - глубже нет смысла идти 
            if (iter == 0 || nearest.Material == Material.Base)
            {
                return color;
            }


            Point3D reflRay = new Point3D();
            if (nearest.Material == Material.Mirror)
            {
                reflRay = Normalize(Reflect(ray, normal));
            }
            else if (nearest.Material == Material.Transparent)
            {
                reflRay = Normalize(Refract(ray, normal));
            }
            Color reflColor = RayTrace(pointIntersec, reflRay, iter - 1);

            double reflectiveFactor = 0.6;
            double localReflectiveFactor = 1 - reflectiveFactor;

            return Color.FromArgb(
                (int)(color.R * localReflectiveFactor + reflColor.R * reflectiveFactor),
                (int)(color.G * localReflectiveFactor + reflColor.G * reflectiveFactor),
                (int)(color.B * localReflectiveFactor + reflColor.B * reflectiveFactor)
            );
        }

        // Находит ближайший объект к лучу и расстояние до него
        private (Figure, double) NearestElem(Point3D eye, Point3D ray)
        {
            (Figure nearest, double min) = (null, double.PositiveInfinity);

            foreach (Figure sceneElement in scene)
            {
                double t = Intersection(eye, ray, sceneElement);
                if (!double.IsPositiveInfinity(t) && (t < min))
                    (nearest, min) = (sceneElement, t);
            }
            return (nearest, min);
        }

        private double CalcIntensity(Point3D point, Point3D normal)
        {
            double intensity = 0;
            foreach (var light in lights)
            {
                if (light.lightType == LightType.Ambient && lights.Count > 1)
                    intensity += light.Intensity;
                else
                {
                    Point3D l = light.Pos - point;

                    (Figure shape, double t) = NearestElem(point, l);

                    if (shape.Type == FigureType.Wall || (shape == null) || t > 1.0)        
                    {
                        double scalar = Scalar(normal, l);                                  // Направление света 
                        if (scalar > 0)
                            intensity += (light.Intensity * scalar) / (Length(normal) * Length(l));
                    }
                    else
                        continue;
                }
            }
            return intensity;
        }

        // Находит пересечение луча с объектом сцены (сторона куба или сфера)
        private double Intersection(Point3D eye, Point3D ray, Figure element)
        {
            if (element.Type == FigureType.Sphere)
            {
                Sphere sphere = element as Sphere;
                Point3D EC = eye - sphere.Center; //луч с началом в Center и направленный в камеру Eye

                double a = Scalar(ray, ray); 
                double b = 2 * Scalar(EC, ray); 
                double c = Scalar(EC, EC) - sphere.Radius * sphere.Radius; 

                double D = b * b - 4 * a * c;
                // Нет корней - нет пересечения
                if (D < eps)
                    return double.PositiveInfinity;

                double t1 = (-b + Math.Sqrt(D)) / (2 * a);
                double t2 = (-b - Math.Sqrt(D)) / (2 * a);

                if (Math.Max(t1, t2) < eps)
                    return double.PositiveInfinity;
                
                return t2 > eps ? t2 : t1;
            }
            else if (element.Type == FigureType.Wall || element.Type == FigureType.SideCube)
            {
                Wall wall = element as Wall;

                Point3D normal = Normalize(wall.Normal);
                Point3D v = eye - wall.MaxPoint;

                double scalar1 = Scalar(v, normal);
                double scalar2 = Scalar(ray, normal);
                double t = -scalar1 / scalar2;

                if (t < eps)
                    return double.PositiveInfinity;

                Point3D interPoint = eye + t * ray;
                return PointInPlane(wall, interPoint) ? t : double.PositiveInfinity;
            }
            return double.PositiveInfinity;
        }

        // Расчет отраженного луча
        private Point3D Reflect(Point3D ray, Point3D normal) => ray - 2 * Scalar(ray, normal) * normal;

        // Расчет преломленного луча
        private Point3D Refract(Point3D ray, Point3D normal)
        {
            const double n1 = 1.1; // Коэф преломления внутри
            const double n2 = 1; // Коэф преломления снаружи объекта
            Point3D norm = Normalize(Scalar(ray, normal) < 0 ? normal : new Point3D(-normal.x, -normal.y, -normal.z));
            double cos_i = -Scalar(norm, Normalize(ray));
            double iorRatio = cos_i > 0 ? n1 / n2 : n2 / n1;
            double cos_t2 = Math.Sqrt(Math.Max(1 - iorRatio * iorRatio * (1 - cos_i * cos_i), 0));

            return new Point3D(ray.x * iorRatio + normal.x * (iorRatio * cos_i - cos_t2),
                               ray.y * iorRatio + normal.y * (iorRatio * cos_i - cos_t2),
                               ray.z * iorRatio + normal.z * (iorRatio * cos_i - cos_t2));
        }
       

        private double Length(Point3D point) => Math.Sqrt(point.x * point.x + point.y * point.y + point.z * point.z);

   
        private Point3D Convert2DTo3D(int x, int y, int width, int height)
        {

            double x3D = (x - width / 2) * (6.0 / width);
            double y3D = -(y - height / 2) * (6.0 / height);
            return new Point3D(x3D, y3D, 5);
        }

        private double Scalar(Point3D v1, Point3D v2) => (v1.x * v2.x) + (v1.y * v2.y) + (v1.z * v2.z);

       
        private Point3D Normalize(Point3D v)
        {
            double len = Math.Sqrt((v.x * v.x) + (v.y * v.y) + (v.z * v.z));
            return new Point3D(v.x / len, v.y / len, v.z / len);
        }

        // Принадлежность точки грани
        private bool PointInPlane(Wall plain, Point3D point) =>
            (point.x < Math.Max(plain.MaxPoint.x, plain.MinPoint.x) + eps) && (point.x > Math.Min(plain.MaxPoint.x, plain.MinPoint.x) - eps) &&
            (point.y < Math.Max(plain.MaxPoint.y, plain.MinPoint.y) + eps) && (point.y > Math.Min(plain.MaxPoint.y, plain.MinPoint.y) - eps) &&
            (point.z < Math.Max(plain.MaxPoint.z, plain.MinPoint.z) + eps) && (point.z > Math.Min(plain.MaxPoint.z, plain.MinPoint.z) - eps);
    }
}
