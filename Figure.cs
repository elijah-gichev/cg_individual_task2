using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace CornishRoom
{
    public enum Material { Base, Mirror, Transparent };
    public enum FigureType { Sphere, Wall, SideCube };

    abstract class Figure
    {
        public Color ColorAmbient;
        public Material Material;
        public FigureType Type;

        public Figure(Color ambient, Material material, FigureType type)
        {
            ColorAmbient = ambient;
            Material = material;
            Type = type;
        }
    }

    class Sphere : Figure
    {
        public Point3D Center;
        public double Radius;

        public Sphere(Color ambient, Material material, Point3D c, double r)
                 : base(ambient, material, FigureType.Sphere)
        {
            Center = c;
            Radius = r;
        }
    }

    class Wall : Figure
    {
        public Point3D MinPoint;
        public Point3D MaxPoint;
        public Point3D Normal;
        public Wall(Color ambient, Material material, FigureType type, Point3D p1, Point3D p2, Point3D norm)
                : base(ambient, material, type)
        {
            MinPoint = p1;
            MaxPoint = p2;
            Normal = norm;
        }
    }

    public enum LightType { Direct, Ambient }
    class Light
    {
        public Point3D Pos;
        public double Intensity;
        public LightType lightType;
        public Light(Point3D pos, double intensity, LightType type)
        {
            Pos = pos;
            Intensity = intensity;
            lightType = type;
        }
    }
}
