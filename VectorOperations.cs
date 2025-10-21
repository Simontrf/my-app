using System;

namespace DxfLibrary
{
    public struct Vect
    {
        public double X;
        public double Y;
        public double Z;

        public Vect(double x = 0, double y = 0, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"Vect({X}, {Y}, {Z})";
        }
    }

    public static class VectorOperations
    {
        public static double Scalaire(Vect V1, Vect V2)
        {
            return (V1.X * V2.X) + (V1.Y * V2.Y) + (V1.Z * V2.Z);
        }

        public static Vect Vectoriel(Vect V1, Vect V2)
        {
            return new Vect
            {
                X = V1.Y * V2.Z - V1.Z * V2.Y,
                Y = V1.Z * V2.X - V1.X * V2.Z,
                Z = V1.X * V2.Y - V1.Y * V2.X
            };
        }

        public static Vect Orthogonal(Vect U)
        {
            return new Vect
            {
                X = U.Y,
                Y = -U.X,
                Z = U.Z
            };
        }

        public static Vect Normalise(Vect U)
        {
            double norme = Math.Sqrt(U.X * U.X + U.Y * U.Y + U.Z * U.Z);
            if (norme != 0)
            {
                return new Vect
                {
                    X = U.X / norme,
                    Y = U.Y / norme,
                    Z = U.Z / norme
                };
            }
            return new Vect(); // Retourne un vecteur nul si la norme est 0
        }

        public static Vect Soustraction(Vect V1, Vect V2)
        {
            return new Vect
            {
                X = V1.X - V2.X,
                Y = V1.Y - V2.Y,
                Z = V1.Z - V2.Z
            };
        }

        public static Vect Addition(Vect V1, Vect V2)
        {
            return new Vect
            {
                X = V1.X + V2.X,
                Y = V1.Y + V2.Y,
                Z = V1.Z + V2.Z
            };
        }

        public static Vect Produit(double l, Vect U)
        {
            return new Vect
            {
                X = U.X * l,
                Y = U.Y * l,
                Z = U.Z * l
            };
        }

        public static Vect Copie(Vect U)
        {
            return new Vect
            {
                X = U.X,
                Y = U.Y,
                Z = U.Z
            };
        }
    }
}