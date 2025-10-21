using System;
using DxfLibrary;

namespace DxfLibrary
{
    public static class GeometryUtils
    {
        public const double PRECISION = 0.001;

        /// <summary>
        /// Calcule l'intersection entre deux segments (première méthode)
        /// </summary>
        public static bool IntersectionSegment(Ligne l1, Ligne l2, out Point ptCible)
        {
            ptCible = new Point();

            double A = l1.M2.X - l1.M1.X;
            double B = l1.M2.Y - l1.M1.Y;
            double C = l1.M2.Z - l1.M1.Z;

            double D = l2.M2.X - l2.M1.X;
            double E = l2.M2.Y - l2.M1.Y;
            double F = l2.M2.Z - l2.M1.Z;

            double G = l2.M1.X - l1.M1.X;
            double H = l2.M1.Y - l1.M1.Y;
            double I = l2.M1.Z - l1.M1.Z;

            if (G == 0 && H == 0 && I == 0)
            {
                G = l2.M2.X - l1.M1.X;
                H = l2.M2.Y - l1.M1.Y;
                I = l2.M2.Z - l1.M1.Z;
            }

            double Delta = A * (E * I - F * H) - B * (D * I - F * G) + C * (D * H - E * G);

            if (Math.Abs(Delta) < PRECISION)
            {
                Delta = -(C + A) * (E + F) + (C + B) * (F + D);
                if (Math.Abs(Delta) < PRECISION)
                    return false;
                else
                {
                    double Alpha = l2.M1.X + l2.M1.Z - (l1.M1.X + l1.M1.Z);
                    double Beta = l2.M1.Y + l2.M1.Z - (l1.M1.Y + l1.M1.Z);

                    double l1val = (Beta * (F + D) - Alpha * (E + F)) / Delta;
                    double l2val = (Beta * (C + A) - Alpha * (C + B)) / Delta;

                    if (l1val > -PRECISION && l1val < 1 + PRECISION &&
                        l2val > -PRECISION && l2val < 1 + PRECISION)
                    {
                        ptCible.X = l1.M1.X + l1val * (l1.M2.X - l1.M1.X);
                        ptCible.Y = l1.M1.Y + l1val * (l1.M2.Y - l1.M1.Y);
                        ptCible.Z = l1.M1.Z + l1val * (l1.M2.Z - l1.M1.Z);
                        return true;
                    }
                    else
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Calcule l'intersection entre deux segments (deuxième méthode alternative)
        /// </summary>
        public static bool IntersectionSegment2(Ligne l1, Ligne l2, out Point ptCible)
        {
            ptCible = new Point();
            double t1, t2, denom1, denom2, denom3, numerateur, Z1, Z2, X1, X2, Y1, Y2;

            Point A = l1.M1;
            Point B = l1.M2;
            Point C = l2.M1;
            Point D = l2.M2;

            denom1 = (D.Y - C.Y) * (B.X - A.X) - (D.X - C.X) * (B.Y - A.Y);
            denom2 = (D.Y - C.Y) * (B.Z - A.Z) - (D.Z - C.Z) * (B.Y - A.Y);
            denom3 = (D.X - C.X) * (B.Z - A.Z) - (D.Z - C.Z) * (B.X - A.X);

            // Si tous les dénominateurs sont nuls, les segments sont parallèles ou confondus
            if (Math.Abs(denom1) < PRECISION && Math.Abs(denom2) < PRECISION && Math.Abs(denom3) < PRECISION)
                return false;

            // Cas 1: utiliser denom1 (projection XY)
            if (Math.Abs(denom1) > PRECISION)
            {
                numerateur = A.Y * (D.X - C.X) - (D.Y - C.Y) * (A.X - C.X) - (D.X - C.X) * C.Y;
                t1 = numerateur / denom1;

                if (Math.Abs(D.X - C.X) > PRECISION)
                    t2 = ((B.X - A.X) * t1 + A.X - C.X) / (D.X - C.X);
                else if (Math.Abs(D.Z - C.Z) > PRECISION)
                    t2 = ((B.Z - A.Z) * t1 + A.Z - C.Z) / (D.Z - C.Z);
                else
                    t2 = ((B.Y - A.Y) * t1 + A.Y - C.Y) / (D.Y - C.Y);

                if (t1 < -PRECISION || t1 > 1 + PRECISION || t2 < -PRECISION || t2 > 1 + PRECISION)
                    return false;

                Z1 = (B.Z - A.Z) * t1 + A.Z;
                Z2 = (D.Z - C.Z) * t2 + C.Z;

                if (Math.Abs(Z1 - Z2) < PRECISION)
                {
                    ptCible.X = (B.X - A.X) * t1 + A.X;
                    ptCible.Y = (B.Y - A.Y) * t1 + A.Y;
                    ptCible.Z = Z1;
                    return true;
                }
                else
                    return false;
            }
            // Cas 2: utiliser denom2 (projection YZ)
            else if (Math.Abs(denom2) > PRECISION)
            {
                numerateur = A.Y * (D.Z - C.Z) - (D.Y - C.Y) * (A.Z - C.Z) - (D.Z - C.Z) * C.Y;
                t1 = numerateur / denom2;

                if (Math.Abs(D.Z - C.Z) > PRECISION)
                    t2 = ((B.Z - A.Z) * t1 + A.Z - C.Z) / (D.Z - C.Z);
                else if (Math.Abs(D.X - C.X) > PRECISION)
                    t2 = ((B.X - A.X) * t1 + A.X - C.X) / (D.X - C.X);
                else
                    t2 = ((B.Y - A.Y) * t1 + A.Y - C.Y) / (D.Y - C.Y);

                if (t1 < -PRECISION || t1 > 1 + PRECISION || t2 < -PRECISION || t2 > 1 + PRECISION)
                    return false;

                X1 = (B.X - A.X) * t1 + A.X;
                X2 = (D.X - C.X) * t2 + C.X;

                if (Math.Abs(X1 - X2) < PRECISION)
                {
                    ptCible.X = X1;
                    ptCible.Y = (B.Y - A.Y) * t1 + A.Y;
                    ptCible.Z = (B.Z - A.Z) * t1 + A.Z;
                    return true;
                }
                else
                    return false;
            }
            // Cas 3: utiliser denom3 (projection XZ)
            else
            {
                numerateur = A.X * (D.Z - C.Z) - (D.X - C.X) * (A.Z - C.Z) - (D.Z - C.Z) * C.X;
                t1 = numerateur / denom3;

                if (Math.Abs(D.Z - C.Z) > PRECISION)
                    t2 = ((B.Z - A.Z) * t1 + A.Z - C.Z) / (D.Z - C.Z);
                else if (Math.Abs(D.Y - C.Y) > PRECISION)
                    t2 = ((B.Y - A.Y) * t1 + A.Y - C.Y) / (D.Y - C.Y);
                else
                    t2 = ((B.X - A.X) * t1 + A.X - C.X) / (D.X - C.X);

                if (t1 < -PRECISION || t1 > 1 + PRECISION || t2 < -PRECISION || t2 > 1 + PRECISION)
                    return false;

                Y1 = (B.Y - A.Y) * t1 + A.Y;
                Y2 = (D.Y - C.Y) * t2 + C.Y;

                if (Math.Abs(Y1 - Y2) < PRECISION)
                {
                    ptCible.X = (B.X - A.X) * t1 + A.X;
                    ptCible.Y = Y1;
                    ptCible.Z = (B.Z - A.Z) * t1 + A.Z;
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Vérifie si un point C est sur le segment AB
        /// </summary>
        public static bool PointSurSegment(Ligne lAB, Point C)
        {
            Vect AB = new Vect
            {
                X = lAB.M2.X - lAB.M1.X,
                Y = lAB.M2.Y - lAB.M1.Y,
                Z = lAB.M2.Z - lAB.M1.Z
            };

            Vect AC = new Vect
            {
                X = C.X - lAB.M1.X,
                Y = C.Y - lAB.M1.Y,
                Z = C.Z - lAB.M1.Z
            };

            double scalaire = VectorOperations.Scalaire(AB, AC);
            double scalaireAB = VectorOperations.Scalaire(AB, AB);

            if (scalaire >= -PRECISION && scalaire <= scalaireAB + PRECISION)
            {
                Vect vect = VectorOperations.Vectoriel(AB, AC);
                if (Math.Abs(vect.X) < PRECISION && Math.Abs(vect.Y) < PRECISION && Math.Abs(vect.Z) < PRECISION)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Calcule la distance au carré entre deux points (évite le calcul de la racine carrée)
        /// </summary>
        public static double Distance2(Point A, Point B)
        {
            double dx = B.X - A.X;
            double dy = B.Y - A.Y;
            double dz = B.Z - A.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Calcule la distance entre deux points
        /// </summary>
        public static double Distance(Point A, Point B)
        {
            return Math.Sqrt(Distance2(A, B));
        }
    }
}