using System;
using System.Collections.Generic;

namespace DxfLibrary
{
    public static class DxfConstants
    {
        public const int BYLAYER = -1;
        public const double PRECISION = 0.001;
        public const int FALSE = 0;
        public const int TRUE = 1;
        public const int MEMERROR = 2;
        public const int EMPTY = 4;
    }

    public delegate void AscenceurProc(long param1, long param2);

    internal class EltListe<T> where T : class
    {
        public EltListe<T> Suivant { get; set; }
        public T Element { get; set; }

        public EltListe(T element = null, EltListe<T> suivant = null)
        {
            Element = element;
            Suivant = suivant;
        }
    }

    public class Liste<T> where T : class
    {
        private EltListe<T> premier;
        private EltListe<T> dernier;
        private int oldIndex;
        private EltListe<T> oldp;

        public int Len { get; private set; }

        public Liste()
        {
            premier = null;
            dernier = null;
            Len = 0;
            oldIndex = -1;
            oldp = null;
        }

        public Liste(Liste<T> other)
        {
            premier = null;
            dernier = null;
            Len = 0;
            oldIndex = -1;
            oldp = null;

            EltListe<T> current = other.premier;
            while (current != null)
            {
                Add(current.Element);
                current = current.Suivant;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Len)
                    throw new IndexOutOfRangeException();

                EltListe<T> current;

                if (oldIndex >= 0 && oldp != null && index >= oldIndex)
                {
                    current = oldp;
                    for (int i = oldIndex; i < index; i++)
                    {
                        current = current.Suivant;
                    }
                }
                else
                {
                    current = premier;
                    for (int i = 0; i < index; i++)
                    {
                        current = current.Suivant;
                    }
                }

                oldIndex = index;
                oldp = current;
                return current.Element;
            }
            set
            {
                if (index < 0 || index >= Len)
                    throw new IndexOutOfRangeException();

                EltListe<T> current = premier;
                for (int i = 0; i < index; i++)
                {
                    current = current.Suivant;
                }
                current.Element = value;
            }
        }

        public T Add(T element)
        {
            if (element == null)
                return null;

            EltListe<T> nouvelElement = new EltListe<T>(element);

            if (premier == null)
            {
                premier = nouvelElement;
                dernier = nouvelElement;
            }
            else
            {
                dernier.Suivant = nouvelElement;
                dernier = nouvelElement;
            }

            Len++;
            oldIndex = -1;
            oldp = null;

            return element;
        }

        public T AddRange(Liste<T> other)
        {
            if (other == null || other.Len == 0)
                return null;

            T premierAjoute = null;
            EltListe<T> current = other.premier;

            while (current != null)
            {
                T elementAjoute = Add(current.Element);
                if (premierAjoute == null)
                    premierAjoute = elementAjoute;
                current = current.Suivant;
            }

            return premierAjoute;
        }

        public T Assign(Liste<T> other)
        {
            Zap();
            return AddRange(other);
        }

        public void Zap()
        {
            premier = null;
            dernier = null;
            Len = 0;
            oldIndex = -1;
            oldp = null;
        }

        public List<T> GetList()
        {
            List<T> result = new List<T>();
            EltListe<T> current = premier;
            while (current != null)
            {
                result.Add(current.Element);
                current = current.Suivant;
            }
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            EltListe<T> current = premier;
            while (current != null)
            {
                yield return current.Element;
                current = current.Suivant;
            }
        }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point(double x = 0, double y = 0, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point(Point other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public static bool operator ==(Point p1, Point p2)
        {
            if (ReferenceEquals(p1, p2)) return true;
            if (p1 is null || p2 is null) return false;
            return Math.Abs(p1.X - p2.X) < DxfConstants.PRECISION &&
                   Math.Abs(p1.Y - p2.Y) < DxfConstants.PRECISION &&
                   Math.Abs(p1.Z - p2.Z) < DxfConstants.PRECISION;
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            return obj is Point point && this == point;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"Point({X:F3}, {Y:F3}, {Z:F3})";
        }
    }

    public class XData
    {
        public string TitreApp { get; set; }
        public string Valeur { get; set; }

        public XData(string titreApp = "XData", string valeur = "")
        {
            TitreApp = titreApp ?? "XData";
            Valeur = valeur ?? "";
        }

        public XData(XData other)
        {
            TitreApp = other.TitreApp ?? "XData";
            Valeur = other.Valeur ?? "";
        }

        public override string ToString()
        {
            return $"XData: {TitreApp} = {Valeur}";
        }
    }

    public class Polyligne
    {
        public int Couleur { get; set; }
        public string NoEnt { get; set; }
        public Liste<Point> Points { get; set; }

        public Polyligne(int couleur = DxfConstants.BYLAYER)
        {
            Couleur = couleur;
            NoEnt = string.Empty;
            Points = new Liste<Point>();
        }

        public Polyligne(Polyligne other)
        {
            Couleur = other.Couleur;
            NoEnt = other.NoEnt ?? "";
            Points = new Liste<Point>(other.Points);
        }

        public Point AddPoint(Point point)
        {
            return Points.Add(point);
        }

        public override string ToString()
        {
            return $"Polyligne: Couleur={Couleur}, Points={Points.Len}";
        }
    }

    public class Face
    {
        public int Couleur { get; set; }
        public string NoEnt { get; set; }
        public Liste<Point> Points { get; set; }

        public Face(int couleur = DxfConstants.BYLAYER)
        {
            Couleur = couleur;
            NoEnt = string.Empty;
            Points = new Liste<Point>();
        }

        public Face(Face other)
        {
            Couleur = other.Couleur;
            NoEnt = other.NoEnt ?? "";
            Points = new Liste<Point>(other.Points);
        }

        public Point AddPoint(Point point)
        {
            return Points.Add(point);
        }

        public override string ToString()
        {
            return $"Face: Couleur={Couleur}, Points={Points.Len}";
        }
    }

    public class Ligne
    {
        public Point M1 { get; set; }
        public Point M2 { get; set; }
        public int Couleur { get; set; }
        public string NoEnt { get; set; }
        public Liste<XData> XDatas { get; set; }
        public Plan AncienPlan { get; set; }

        public Ligne(Point m1 = null, Point m2 = null, int couleur = DxfConstants.BYLAYER)
        {
            M1 = m1 ?? new Point(0, 0, 0);
            M2 = m2 ?? new Point(0, 0, 0);
            Couleur = couleur;
            NoEnt = string.Empty;
            XDatas = new Liste<XData>();
            AncienPlan = null;
        }

        public Ligne(Ligne other)
        {
            M1 = new Point(other.M1);
            M2 = new Point(other.M2);
            Couleur = other.Couleur;
            NoEnt = other.NoEnt ?? "";
            XDatas = new Liste<XData>(other.XDatas);
            AncienPlan = other.AncienPlan;
        }

        public XData AddXData(XData xdata)
        {
            return XDatas.Add(xdata);
        }

        public static bool operator ==(Ligne l1, Ligne l2)
        {
            if (ReferenceEquals(l1, l2)) return true;
            if (l1 is null || l2 is null) return false;
            return l1.M1 == l2.M1 && l1.M2 == l2.M2;
        }

        public static bool operator !=(Ligne l1, Ligne l2)
        {
            return !(l1 == l2);
        }

        public override bool Equals(object obj)
        {
            return obj is Ligne ligne && this == ligne;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(M1, M2, Couleur);
        }

        public override string ToString()
        {
            return $"Ligne: M1={M1}, M2={M2}, Couleur={Couleur}";
        }
    }
    public class Texte
    {
        public Point M { get; set; }
        public string TexteStr { get; set; }
        public double Hauteur { get; set; }
        public double Orient { get; set; }
        public int Couleur { get; set; }
        public string NoEnt { get; set; }

        public Texte(Point m = null, string texte = "", double hauteur = 0,
                     double orient = 0, int couleur = DxfConstants.BYLAYER)
        {
            M = m ?? new Point(0, 0, 0);
            TexteStr = texte ?? "";
            Hauteur = hauteur;
            Orient = orient;
            Couleur = couleur;
            NoEnt = string.Empty;
        }

        public Texte(Texte other)
        {
            M = new Point(other.M);
            TexteStr = other.TexteStr ?? "";
            Hauteur = other.Hauteur;
            Orient = other.Orient;
            Couleur = other.Couleur;
            NoEnt = other.NoEnt ?? "";
        }

        public override string ToString()
        {
            return $"Texte: '{TexteStr}' à {M}";
        }
    }

    public class Block
    {
        public Point M { get; set; }
        public string Nom { get; set; }
        public int Couleur { get; set; }
        public string NoEnt { get; set; }
        public double Angle { get; set; }
        public double EchelleX { get; set; }
        public double EchelleY { get; set; }
        public double EchelleZ { get; set; }
        public Liste<XData> XDatas { get; set; }

        public Block(Point m = null, string nom = "NO_NAME", int couleur = DxfConstants.BYLAYER,
                     double angle = 0, double echelleX = 1, double echelleY = 1, double echelleZ = 1)
        {
            M = m ?? new Point(0, 0, 0);
            Nom = nom ?? "NO_NAME";
            Couleur = couleur;
            NoEnt = string.Empty;
            Angle = angle;
            EchelleX = echelleX;
            EchelleY = echelleY;
            EchelleZ = echelleZ;
            XDatas = new Liste<XData>();
        }

        public Block(Block other)
        {
            M = new Point(other.M);
            Nom = other.Nom ?? "NO_NAME";
            Couleur = other.Couleur;
            NoEnt = other.NoEnt ?? "";
            Angle = other.Angle;
            EchelleX = other.EchelleX;
            EchelleY = other.EchelleY;
            EchelleZ = other.EchelleZ;
            XDatas = new Liste<XData>(other.XDatas);
        }

        public XData AddXData(XData xdata)
        {
            return XDatas.Add(xdata);
        }

        public override string ToString()
        {
            return $"Block: {Nom} à {M}";
        }
    }

    public class Plan
    {
        public int Couleur { get; set; }
        public string Nom { get; set; }
        public Liste<Polyligne> Polylignes { get; set; }
        public Liste<Ligne> Lignes { get; set; }
        public Liste<Face> Faces { get; set; }
        public Liste<Texte> Textes { get; set; }
        public Liste<Block> Blocks { get; set; }

        public Plan(string nom = "", int couleur = 0)
        {
            Nom = nom ?? "";
            Couleur = couleur;
            Polylignes = new Liste<Polyligne>();
            Lignes = new Liste<Ligne>();
            Faces = new Liste<Face>();
            Textes = new Liste<Texte>();
            Blocks = new Liste<Block>();
        }

        public Plan(Plan other)
        {
            Nom = other.Nom ?? "";
            Couleur = other.Couleur;
            Polylignes = new Liste<Polyligne>(other.Polylignes);
            Lignes = new Liste<Ligne>(other.Lignes);
            Faces = new Liste<Face>(other.Faces);
            Textes = new Liste<Texte>(other.Textes);
            Blocks = new Liste<Block>(other.Blocks);
        }

        public bool Vide()
        {
            return Polylignes.Len == 0 && Lignes.Len == 0 &&
                   Faces.Len == 0 && Textes.Len == 0 && Blocks.Len == 0;
        }

        public Polyligne Add(Polyligne polyligne)
        {
            return Polylignes.Add(polyligne);
        }

        public Ligne Add(Ligne ligne)
        {
            return Lignes.Add(ligne);
        }

        public Block Add(Block block)
        {
            return Blocks.Add(block);
        }

        public Face Add(Face face)
        {
            return Faces.Add(face);
        }

        public Texte Add(Texte texte)
        {
            return Textes.Add(texte);
        }

        public Plan AddPlan(Plan other)
        {
            if (other == null) return null;

            Polylignes.AddRange(other.Polylignes);
            Lignes.AddRange(other.Lignes);
            Faces.AddRange(other.Faces);
            Textes.AddRange(other.Textes);
            Blocks.AddRange(other.Blocks);

            return this;
        }

        public Plan Assign(Plan other)
        {
            if (other == null) return null;

            Nom = other.Nom;
            Couleur = other.Couleur;

            Polylignes.Zap();
            Lignes.Zap();
            Faces.Zap();
            Textes.Zap();
            Blocks.Zap();

            return AddPlan(other);
        }

        public override string ToString()
        {
            return $"Plan: {Nom} (Lignes={Lignes.Len}, Polylignes={Polylignes.Len}, Faces={Faces.Len}, Textes={Textes.Len}, Blocks={Blocks.Len})";
        }
    }

    public partial class Dessin
    {
        public string Fichier { get; set; }
        public Liste<Plan> Plans { get; set; }

        public Dessin(string fichier = "")
        {
            Fichier = fichier ?? "";
            Plans = new Liste<Plan>();
        }

        public Dessin(Dessin other)
        {
            Fichier = other.Fichier ?? "";
            Plans = new Liste<Plan>(other.Plans);
        }

        public Plan GetPlan(string nom)
        {
            for (int i = 0; i < Plans.Len; i++)
            {
                if (Plans[i].Nom == nom)
                    return Plans[i];
            }

            // Si le plan n'existe pas, le créer
            Plan nouveauPlan = new Plan(nom);
            Plans.Add(nouveauPlan);
            return nouveauPlan;
        }

        public Plan Add(Plan plan)
        {
            return Plans.Add(plan);
        }

        public override string ToString()
        {
            return $"Dessin: {Fichier} ({Plans.Len} plans)";
        }
    }

    public class Extremite
    {
        public int Amont { get; set; }
        public Point Point { get; set; }
        public bool Trouve { get; set; }

        public Extremite(int amont = 0, Point point = null)
        {
            Amont = amont;
            Point = point ?? new Point(0, 0, 0);
            Trouve = false;
        }

        public Extremite(Extremite other)
        {
            Amont = other.Amont;
            Point = new Point(other.Point);
            Trouve = other.Trouve;
        }

        public override string ToString()
        {
            return $"Extremite: Amont={Amont}, Point={Point}, Trouve={Trouve}";
        }
    }
}