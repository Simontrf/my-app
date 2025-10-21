using System;
using System.IO;
using System.Text;

namespace DxfLibrary
{
    // ============================================================================
    // IMPLÉMENTATIONS COMPLÈTES DES CLASSES (depuis DXF.CPP)
    // ============================================================================

    // Note: Les classes de base (Point, Liste, etc.) sont déjà dans dxf_h.cs
    // Ce fichier contient les méthodes métier importantes

    public static class DxfUtilities
    {
        //Découpe une ligne à un point donné
        public static void CoupeLigne(Plan plan, Ligne ligne, Point m)
        {
            if (GeometryUtils.PointSurSegment(ligne, m))
            {
                if (ligne.M2 != m && ligne.M1 != m)
                {
                    Ligne nouvelleLigne = plan.Add(new Ligne(ligne.M1, m));
                    nouvelleLigne.AncienPlan = ligne.AncienPlan;
                    nouvelleLigne.Couleur = ligne.Couleur;
                    nouvelleLigne.XDatas = new Liste<XData>(ligne.XDatas);
                    ligne.M1 = m;
                }
            }
        }

        //Calcule la longueur d'une ligne
        public static double Longueur(Ligne ligne)
        {
            return Math.Sqrt(
                (ligne.M2.X - ligne.M1.X) * (ligne.M2.X - ligne.M1.X) +
                (ligne.M2.Y - ligne.M1.Y) * (ligne.M2.Y - ligne.M1.Y) +
                (ligne.M2.Z - ligne.M1.Z) * (ligne.M2.Z - ligne.M1.Z)
            );
        }

        //Calcule le sens/angle d'une ligne
        public static double Sens(Ligne ligne)
        {
            if ((ligne.M2.Z - ligne.M1.Z) > 0.0001)
                return +1;
            else if ((ligne.M2.Z - ligne.M1.Z) < -0.0001)
                return -1;
            else
            {
                double angle = Math.Atan2(
                    ligne.M2.Y - ligne.M1.Y,
                    ligne.M2.X - ligne.M1.X
                ) / Math.PI * 180;

                double ancienAngle = angle;

                // Corrections d'angle (logique Borland conservée)
                if (angle > 0.9 && angle < 1)
                    angle = 0.9;
                if (angle >= 1 && angle <= 1.1)
                    angle = 1.1;
                if (angle < -0.9 && angle > -1)
                    angle = -0.9;
                if (angle <= -1 && angle >= -1.1)
                    angle = -1.1;

                return angle;
            }
        }

        //Vérifie si deux chaînes commencent de la même façon
        public static bool CommencePareil(string chaine1, string chaine2)
        {
            int len = chaine1.Length;
            if (len > chaine2.Length)
                return false;

            for (int i = 0; i < len; i++)
            {
                if (chaine1[i] != chaine2[i])
                    return false;
            }
            return true;
        }

        //Écrit l'en-tête du fichier d'erreurs
        public static void EcritTeteErreur(StreamWriter writer)
        {
            writer.Write(@"  0
SECTION
  2
HEADER
  9
$CLAYER
  8
ERREURS
  9
$INSUNITS
 70
     6
  0
ENDSEC
  0
SECTION
  2
ENTITIES
");
        }

        //Écrit une erreur dans le fichier DXF d'erreurs
        public static void EcritErreur(StreamWriter dxfWriter, StreamWriter txtWriter,
                                       double x, double y, double z, string texte)
        {
            // Croix d'erreur en DXF
            dxfWriter.Write($@"  0
LINE
  8
ERREURS
 10
{x:F2}
 20
{y:F2}
 30
{z:F2}
 11
{x + 0.5:F2}
 21
{y + 0.5:F2}
 31
{z:F2}
  0
LINE
  8
ERREURS
 10
{x:F2}
 20
{y:F2}
 30
{z:F2}
 11
{x + 0.03:F2}
 21
{y + 0.12:F2}
 31
{z:F2}
  0
LINE
  8
ERREURS
 10
{x:F2}
 20
{y:F2}
 30
{z:F2}
 11
{x + 0.12:F2}
 21
{y + 0.03:F2}
 31
{z:F2}
  0
TEXT
  8
ERREURS
 10
{x + 0.5:F2}
 20
{y + 0.5:F2}
 30
{z:F2}
 40
0.1
  1
Err.
");

            // Fichier texte d'erreurs
            txtWriter.WriteLine($"{x:F2},{y:F2},{z:F2},\"{texte}\"");
        }

        //Écrit le pied du fichier d'erreurs
        public static void EcritPiedErreur(StreamWriter writer)
        {
            writer.Write(@"  0
ENDSEC
  0
EOF
");
        }
    }

    // ============================================================================
    // EXTENSIONS DE LA CLASSE DESSIN (méthodes principales)
    // ============================================================================

    public partial class Dessin
    {
        private static string targetDir = "";

        public static void SetTargetDirRoutines(string inTargetDir)
        {
            targetDir = inTargetDir ?? "";
            if (targetDir.Length > 0 && !targetDir.EndsWith("\\"))
            {
                targetDir += "\\";
            }
        }

        //Vérifie si un autre plan correspond au pattern
        public bool AutrePlan(string nomPlan, out string otherLayer, int index)
        {
            otherLayer = "";
            int compteur = 0;

            if (nomPlan.EndsWith("*"))
            {
                string temp = nomPlan.Substring(0, nomPlan.Length - 1);
                for (int i = 0; i < Plans.Len; i++)
                {
                    if (DxfUtilities.CommencePareil(temp, Plans[i].Nom))
                    {
                        compteur++;
                        if (compteur >= index)
                        {
                            otherLayer = Plans[i].Nom;
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (index == 1)
                {
                    for (int i = 0; i < Plans.Len; i++)
                    {
                        if (nomPlan == Plans[i].Nom)
                        {
                            otherLayer = Plans[i].Nom;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //Lit un fichier ligne par ligne (équivalent de lineinput)
        private static string LineInput(StreamReader reader)
        {
            StringBuilder sb = new StringBuilder();
            int ch;

            while ((ch = reader.Read()) != -1 && ch != '\r' && ch != '\n')
            {
                if (sb.Length < 256)
                    sb.Append((char)ch);
            }

            // Consommer le \n si on a trouvé un \r
            if (ch == '\r')
            {
                int nextCh = reader.Peek();
                if (nextCh == '\n')
                    reader.Read();
            }

            return sb.ToString();
        }

        //LIT UN FICHIER DXF COMPLET (implémentation complète depuis DXF.CPP)
        public bool LitDXF(AscenceurProc ascenceur, string fichier)
        {
            if (string.IsNullOrEmpty(fichier))
                return false;

            if (!File.Exists(fichier))
                return false;

            try
            {
                using (StreamReader reader = new StreamReader(fichier))
                {
                    long fileLen = reader.BaseStream.Length;
                    string chaine, oldChaine = "";
                    bool entitiesSection = false;
                    Block lastBlock = null;

                    ascenceur?.Invoke(fileLen, 0);

                    while (!reader.EndOfStream)
                    {
                        chaine = LineInput(reader);
                        long currentPos = reader.BaseStream.Position;
                        ascenceur?.Invoke(fileLen, currentPos);

                        if (oldChaine == "  0")
                        {
                            // ========== LAYERS (Plans) ==========
                            if (chaine == "LAYER")
                            {
                                Plan plan = new Plan();
                                while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                {
                                    ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                    if (chaine == "  2") // Nom
                                        plan.Nom = LineInput(reader);
                                    else if (chaine == " 62") // Couleur
                                        plan.Couleur = int.Parse(LineInput(reader));
                                    else if (chaine == " 70" || chaine == "  6")
                                        LineInput(reader); // Ignorer
                                }
                                Add(plan);
                            }

                            // ========== SECTIONS ==========
                            if (chaine == "SECTION")
                            {
                                while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  2")
                                { }

                                if (!reader.EndOfStream)
                                {
                                    chaine = LineInput(reader);
                                    if (chaine == "ENTITIES")
                                        entitiesSection = true;
                                }
                            }

                            if (chaine == "ENDSEC")
                                entitiesSection = false;

                            if (entitiesSection)
                            {
                                // ========== LIGNES ==========
                                if (chaine == "LINE")
                                {
                                    Ligne ligne = new Ligne();
                                    string xDataTitreApp = "";
                                    string nomPlan = "";

                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                    {
                                        ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                        if (chaine == "  8") // Layer
                                            nomPlan = LineInput(reader);
                                        else if (chaine == " 10") // X1
                                            ligne.M1.X = double.Parse(LineInput(reader));
                                        else if (chaine == " 20") // Y1
                                            ligne.M1.Y = double.Parse(LineInput(reader));
                                        else if (chaine == " 30") // Z1
                                            ligne.M1.Z = double.Parse(LineInput(reader));
                                        else if (chaine == " 11") // X2
                                            ligne.M2.X = double.Parse(LineInput(reader));
                                        else if (chaine == " 21") // Y2
                                            ligne.M2.Y = double.Parse(LineInput(reader));
                                        else if (chaine == " 31") // Z2
                                            ligne.M2.Z = double.Parse(LineInput(reader));
                                        else if (chaine == " 62") // Couleur
                                            ligne.Couleur = int.Parse(LineInput(reader));
                                        else if (chaine == "  5") // noEnt
                                            ligne.NoEnt = LineInput(reader);
                                        else if (chaine == "  6") // Type ligne
                                            LineInput(reader);
                                        else if (chaine == "1001") // XData App
                                            xDataTitreApp = LineInput(reader);
                                        else if (chaine == "1000") // XData Value
                                            ligne.AddXData(new XData(xDataTitreApp, LineInput(reader)));
                                    }

                                    // Ne pas prendre les lignes vides
                                    if (GeometryUtils.Distance2(ligne.M1, ligne.M2) > DxfConstants.PRECISION)
                                    {
                                        GetPlan(nomPlan).Add(ligne);
                                    }
                                }

                                // ========== POLYLIGNES ==========
                                else if (chaine == "POLYLINE")
                                {
                                    Polyligne polyligne = new Polyligne();
                                    string nomPlan = "";

                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                    {
                                        ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                        if (chaine == "  8") // Layer
                                            nomPlan = LineInput(reader);
                                        else if (chaine == " 62") // Couleur
                                            polyligne.Couleur = int.Parse(LineInput(reader));
                                        else if (chaine == "  5") // noEnt
                                            polyligne.NoEnt = LineInput(reader);
                                        else if (chaine == " 66" || chaine == " 70" || chaine == "  6")
                                            LineInput(reader); // Ignorer
                                    }

                                    // Lire les VERTEX
                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "SEQEND")
                                    {
                                        Point m = new Point();
                                        while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                        {
                                            ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                            if (chaine == " 10") // X
                                                m.X = double.Parse(LineInput(reader));
                                            else if (chaine == " 20") // Y
                                                m.Y = double.Parse(LineInput(reader));
                                            else if (chaine == " 30") // Z
                                                m.Z = double.Parse(LineInput(reader));
                                            else if (chaine == "  8" || chaine == " 70")
                                                LineInput(reader);
                                        }
                                        polyligne.AddPoint(m);
                                    }
                                    GetPlan(nomPlan).Add(polyligne);
                                }

                                // ========== FACES 3D ==========
                                else if (chaine == "3DFACE")
                                {
                                    Face face = new Face();
                                    Point m1 = new Point(), m2 = new Point(), m3 = new Point(), m4 = new Point();
                                    bool bM1 = false, bM2 = false, bM3 = false, bM4 = false;
                                    string nomPlan = "";

                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                    {
                                        ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                        if (chaine == "  8") nomPlan = LineInput(reader);
                                        else if (chaine == " 10") { m1.X = double.Parse(LineInput(reader)); bM1 = true; }
                                        else if (chaine == " 20") { m1.Y = double.Parse(LineInput(reader)); bM1 = true; }
                                        else if (chaine == " 30") { m1.Z = double.Parse(LineInput(reader)); bM1 = true; }
                                        else if (chaine == " 11") { m2.X = double.Parse(LineInput(reader)); bM2 = true; }
                                        else if (chaine == " 21") { m2.Y = double.Parse(LineInput(reader)); bM2 = true; }
                                        else if (chaine == " 31") { m2.Z = double.Parse(LineInput(reader)); bM2 = true; }
                                        else if (chaine == " 12") { m3.X = double.Parse(LineInput(reader)); bM3 = true; }
                                        else if (chaine == " 22") { m3.Y = double.Parse(LineInput(reader)); bM3 = true; }
                                        else if (chaine == " 32") { m3.Z = double.Parse(LineInput(reader)); bM3 = true; }
                                        else if (chaine == " 13") { m4.X = double.Parse(LineInput(reader)); bM4 = true; }
                                        else if (chaine == " 23") { m4.Y = double.Parse(LineInput(reader)); bM4 = true; }
                                        else if (chaine == " 33") { m4.Z = double.Parse(LineInput(reader)); bM4 = true; }
                                        else if (chaine == " 62") face.Couleur = int.Parse(LineInput(reader));
                                        else if (chaine == "  5") face.NoEnt = LineInput(reader);
                                        else if (chaine == "  6") LineInput(reader);
                                    }

                                    if (bM1) face.AddPoint(m1);
                                    if (bM2) face.AddPoint(m2);
                                    if (bM3) face.AddPoint(m3);
                                    if (bM4) face.AddPoint(m4);

                                    GetPlan(nomPlan).Add(face);
                                }

                                // ========== TEXTES ==========
                                else if (chaine == "TEXT")
                                {
                                    Texte texte = new Texte();
                                    string nomPlan = "";

                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                    {
                                        ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                        if (chaine == "  8") nomPlan = LineInput(reader);
                                        else if (chaine == " 10") texte.M.X = double.Parse(LineInput(reader));
                                        else if (chaine == " 20") texte.M.Y = double.Parse(LineInput(reader));
                                        else if (chaine == " 30") texte.M.Z = double.Parse(LineInput(reader));
                                        else if (chaine == "  1") texte.TexteStr = LineInput(reader);
                                        else if (chaine == " 40") texte.Hauteur = double.Parse(LineInput(reader));
                                        else if (chaine == " 50") texte.Orient = double.Parse(LineInput(reader));
                                        else if (chaine == " 62") texte.Couleur = int.Parse(LineInput(reader));
                                        else if (chaine == "  5") texte.NoEnt = LineInput(reader);
                                        else if (chaine == "  6") LineInput(reader);
                                    }
                                    GetPlan(nomPlan).Add(texte);
                                }

                                // ========== BLOCKS (INSERT) ==========
                                else if (chaine == "INSERT")
                                {
                                    Block block = new Block();
                                    string xDataTitreApp = "";
                                    string nomPlan = "";
                                    bool dejaX = false, dejaY = false, dejaZ = false;

                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                    {
                                        ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                        if (chaine == "  8") nomPlan = LineInput(reader);
                                        else if (chaine == " 10" && !dejaX) { block.M.X = double.Parse(LineInput(reader)); dejaX = true; }
                                        else if (chaine == " 20" && !dejaY) { block.M.Y = double.Parse(LineInput(reader)); dejaY = true; }
                                        else if (chaine == " 30" && !dejaZ) { block.M.Z = double.Parse(LineInput(reader)); dejaZ = true; }
                                        else if (chaine == "  2") block.Nom = LineInput(reader);
                                        else if (chaine == " 62") block.Couleur = int.Parse(LineInput(reader));
                                        else if (chaine == "  5") block.NoEnt = LineInput(reader);
                                        else if (chaine == "1001") xDataTitreApp = LineInput(reader);
                                        else if (chaine == "1000") block.AddXData(new XData(xDataTitreApp, LineInput(reader)));
                                    }
                                    lastBlock = GetPlan(nomPlan).Add(block);
                                }

                                // ========== ATTRIBUTS ==========
                                else if (chaine == "ATTRIB")
                                {
                                    string etiquAttrib = "*";
                                    string valeurAttrib = "";

                                    while (!reader.EndOfStream && (chaine = LineInput(reader)) != "  0")
                                    {
                                        ascenceur?.Invoke(fileLen, reader.BaseStream.Position);

                                        if (chaine == "  2") etiquAttrib += LineInput(reader);
                                        else if (chaine == "  1") valeurAttrib = LineInput(reader);
                                    }

                                    lastBlock?.AddXData(new XData(etiquAttrib, valeurAttrib));
                                }
                            } // if (entitiesSection)
                        } // if (oldChaine == "  0")

                        oldChaine = chaine;
                    } // while (!reader.EndOfStream)

                    ascenceur?.Invoke(fileLen, fileLen + 1);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        //ÉCRIT LES TRONÇONS (implémentation complète depuis DXF.CPP)
        public int EcrisTroncon(AscenceurProc ascenceur, string fichier, string nomPlan, bool needXDatas)
        {
            // Chemins des fichiers
            string dxfHdr = Path.Combine(targetDir, "Dxf.hdr");
            string fichierOut = Path.Combine(targetDir, fichier);
            string dxfTxt = Path.Combine(targetDir, "Dxf.txt");
            string erreurDxf = Path.Combine(targetDir, "Erreurs.dxf");
            string erreurTxt = Path.Combine(targetDir, "Erreurs.txt");

            try
            {
                using (StreamWriter hFileHdr = new StreamWriter(dxfHdr))
                using (StreamWriter hFile = new StreamWriter(fichierOut))
                using (StreamWriter hFileTxt = new StreamWriter(dxfTxt))
                using (StreamWriter hErr = new StreamWriter(erreurDxf))
                using (StreamWriter hErr2 = new StreamWriter(erreurTxt))
                {
                    bool err = false;

                    // Écrire l'en-tête
                    hFileHdr.WriteLine("AMONT, N, 5, 0");
                    hFileHdr.WriteLine("TRONCON, N, 5, 0");
                    hFileHdr.WriteLine("X1, N, 10, 3");
                    hFileHdr.WriteLine("Y1, N, 10, 3");
                    hFileHdr.WriteLine("Z1, N, 10, 3");
                    hFileHdr.WriteLine("X2, N, 10, 3");
                    hFileHdr.WriteLine("Y2, N, 10, 3");
                    hFileHdr.WriteLine("Z2, N, 10, 3");
                    hFileHdr.WriteLine("COUL, N, 5, 0");
                    hFileHdr.WriteLine("LONG, N, 10, 3");
                    hFileHdr.WriteLine("SENS, N, 7, 3");
                    hFileHdr.WriteLine("TXTDEBUT, C, 20, 0");
                    hFileHdr.WriteLine("TXTFIN, C, 20, 0");
                    hFileHdr.WriteLine("TXTLONG, C, 30, 0");
                    hFileHdr.WriteLine("PLAN, C, 30, 0");
                    hFileHdr.WriteLine("ENTITYNAME, C, 30, 0");

                    DxfUtilities.EcritTeteErreur(hErr);

                    // Construire le plan à traiter
                    Plan plan = new Plan("xxxx", 0);
                    string otherLayer;
                    int iiii = 0;

                    while (AutrePlan(nomPlan, out otherLayer, ++iiii))
                    {
                        Plan planTemp = GetPlan(otherLayer);
                        if (!planTemp.Vide())
                        {
                            plan.AddPlan(planTemp);
                        }
                    }

                    // Virer les lignes de longueur 0
                    for (int ii = 0; ii < plan.Lignes.Len; ii++)
                    {
                        if (GeometryUtils.Distance2(plan.Lignes[ii].M1, plan.Lignes[ii].M2) <= DxfConstants.PRECISION)
                        {
                            plan.Lignes[ii].M1 = new Point(0, 0, 0);
                            plan.Lignes[ii].M2 = new Point(0, 0, 0);
                        }
                    }

                    // Coupe selon les blocs
                    for (int i = 0; i < plan.Blocks.Len; i++)
                    {
                        Block block = plan.Blocks[i];
                        for (int ii = 0; ii < plan.Lignes.Len; ii++)
                        {
                            if (GeometryUtils.PointSurSegment(plan.Lignes[ii], block.M))
                            {
                                if (GeometryUtils.Distance2(plan.Lignes[ii].M1, plan.Lignes[ii].M2) > DxfConstants.PRECISION)
                                {
                                    DxfUtilities.CoupeLigne(plan, plan.Lignes[ii], block.M);
                                }
                            }
                        }
                    }

                    // Coupe selon les lignes
                    for (int i = 0; i < plan.Lignes.Len; i++)
                    {
                        Ligne ligne = plan.Lignes[i];
                        for (int ii = i + 1; ii < plan.Lignes.Len; ii++)
                        {
                            if (ligne == plan.Lignes[ii])
                            {
                                plan.Lignes[ii].M1 = new Point(0, 0, 0);
                                plan.Lignes[ii].M2 = new Point(0, 0, 0);
                            }
                            else
                            {
                                Point m;
                                if (GeometryUtils.IntersectionSegment(ligne, plan.Lignes[ii], out m))
                                {
                                    DxfUtilities.CoupeLigne(plan, plan.Lignes[ii], m);
                                    DxfUtilities.CoupeLigne(plan, ligne, m);
                                }
                                else if (GeometryUtils.IntersectionSegment2(ligne, plan.Lignes[ii], out m))
                                {
                                    DxfUtilities.CoupeLigne(plan, plan.Lignes[ii], m);
                                    DxfUtilities.CoupeLigne(plan, ligne, m);
                                }
                            }
                        }
                    }

                    // Trouver les points de départ
                    Liste<Extremite> oldExtremite = new Liste<Extremite>();

                    for (int i = 0; i < plan.Textes.Len; i++)
                    {
                        if (plan.Textes[i].TexteStr == "DEPART")
                        {
                            oldExtremite.Add(new Extremite(0, new Point(plan.Textes[i].M)));
                        }
                    }

                    for (int i = 0; i < plan.Blocks.Len; i++)
                    {
                        if (plan.Blocks[i].Nom == "_DEPART")
                        {
                            oldExtremite.Add(new Extremite(0, new Point(plan.Blocks[i].M)));
                        }
                    }

                    ascenceur?.Invoke(plan.Lignes.Len, 0);

                    // Traitement des tronçons
                    int troncon = 0;
                    Liste<Extremite> extremite = new Liste<Extremite>();

                    while (oldExtremite.Len > 0)
                    {
                        for (int i = 0; i < plan.Lignes.Len; i++)
                        {
                            Ligne ligne = plan.Lignes[i];
                            for (int ii = 0; ii < oldExtremite.Len; ii++)
                            {
                                if (ligne.M1 != ligne.M2 &&
                                    (ligne.M1 == oldExtremite[ii].Point || ligne.M2 == oldExtremite[ii].Point))
                                {
                                    troncon++;
                                    oldExtremite[ii].Trouve = true;

                                    ascenceur?.Invoke(plan.Lignes.Len, troncon);

                                    // Inverser la ligne si nécessaire
                                    if (ligne.M2 == oldExtremite[ii].Point)
                                    {
                                        Point m = ligne.M1;
                                        ligne.M1 = ligne.M2;
                                        ligne.M2 = m;
                                    }

                                    Extremite pExtremite = extremite.Add(new Extremite(troncon, ligne.M2));

                                    // Collecter les textes
                                    StringBuilder debutTexte = new StringBuilder();
                                    StringBuilder finTexte = new StringBuilder();
                                    StringBuilder longTexte = new StringBuilder();

                                    // XDatas de la ligne
                                    for (int iii = 0; iii < ligne.XDatas.Len; iii++)
                                    {
                                        longTexte.Append("*");
                                        longTexte.Append(ligne.XDatas[iii].TitreApp);
                                        longTexte.Append("=");
                                        longTexte.Append(ligne.XDatas[iii].Valeur);
                                        longTexte.Append("³");
                                    }

                                    if (needXDatas && ligne.XDatas.Len == 0)
                                    {
                                        DxfUtilities.EcritErreur(hErr, hErr2,
                                            (ligne.M1.X + ligne.M2.X) / 2,
                                            (ligne.M1.Y + ligne.M2.Y) / 2,
                                            (ligne.M1.Z + ligne.M2.Z) / 2,
                                            "Ce tronçon n'a pas de donnée.");
                                        err = true;
                                    }

                                    // Traiter les textes
                                    for (int iii = 0; iii < plan.Textes.Len; iii++)
                                    {
                                        if (!string.IsNullOrEmpty(plan.Textes[iii].TexteStr))
                                        {
                                            if (GeometryUtils.PointSurSegment(ligne, plan.Textes[iii].M))
                                            {
                                                if (ligne.M2 == plan.Textes[iii].M)
                                                {
                                                    finTexte.Append(plan.Textes[iii].TexteStr);
                                                    finTexte.Append("³");
                                                    plan.Textes[iii].TexteStr = "";
                                                    pExtremite.Trouve = true;
                                                }
                                                else if (ligne.M1 == plan.Textes[iii].M)
                                                {
                                                    debutTexte.Append(plan.Textes[iii].TexteStr);
                                                    debutTexte.Append("³");
                                                    plan.Textes[iii].TexteStr = "";
                                                }
                                                else
                                                {
                                                    longTexte.Append(plan.Textes[iii].TexteStr);
                                                    longTexte.Append("³");
                                                    plan.Textes[iii].TexteStr = "";
                                                }
                                            }
                                        }
                                    }

                                    // Traiter les blocs
                                    for (int iii = 0; iii < plan.Blocks.Len; iii++)
                                    {
                                        if (!string.IsNullOrEmpty(plan.Blocks[iii].Nom))
                                        {
                                            if (ligne.M2 == plan.Blocks[iii].M)
                                            {
                                                finTexte.Append("*BLOCK=");
                                                finTexte.Append(plan.Blocks[iii].Nom);
                                                finTexte.Append("³");

                                                for (int xi = 0; xi < plan.Blocks[iii].XDatas.Len; xi++)
                                                {
                                                    finTexte.Append("*");
                                                    finTexte.Append(plan.Blocks[iii].XDatas[xi].TitreApp);
                                                    finTexte.Append("=");
                                                    finTexte.Append(plan.Blocks[iii].XDatas[xi].Valeur);
                                                    finTexte.Append("³");
                                                }

                                                if (needXDatas && plan.Blocks[iii].XDatas.Len == 0)
                                                {
                                                    DxfUtilities.EcritErreur(hErr, hErr2,
                                                        plan.Blocks[iii].M.X,
                                                        plan.Blocks[iii].M.Y,
                                                        plan.Blocks[iii].M.Z,
                                                        "Ce bloc n'a pas de donnée.");
                                                    err = true;
                                                }

                                                pExtremite.Trouve = true;
                                                plan.Blocks[iii].Nom = "";
                                            }
                                            else if (ligne.M1 == plan.Blocks[iii].M &&
                                                     plan.Blocks[iii].Nom == "_DEPART")
                                            {
                                                debutTexte.Append("*BLOCK=");
                                                debutTexte.Append(plan.Blocks[iii].Nom);
                                                debutTexte.Append("³");

                                                for (int xi = 0; xi < plan.Blocks[iii].XDatas.Len; xi++)
                                                {
                                                    debutTexte.Append("*");
                                                    debutTexte.Append(plan.Blocks[iii].XDatas[xi].TitreApp);
                                                    debutTexte.Append("=");
                                                    debutTexte.Append(plan.Blocks[iii].XDatas[xi].Valeur);
                                                    debutTexte.Append("³");
                                                }

                                                plan.Blocks[iii].Nom = "";
                                            }
                                        }
                                    }

                                    // Déterminer la couleur
                                    if (ligne.Couleur == DxfConstants.BYLAYER)
                                    {
                                        ligne.Couleur = ligne.AncienPlan?.Couleur ?? plan.Couleur;
                                    }

                                    // Écrire le tronçon
                                    hFile.WriteLine($"{oldExtremite[ii].Amont}, {troncon}, " +
                                        $"{ligne.M1.X:F3}, {ligne.M1.Y:F3}, {ligne.M1.Z:F3}, " +
                                        $"{ligne.M2.X:F3}, {ligne.M2.Y:F3}, {ligne.M2.Z:F3}, " +
                                        $"{ligne.Couleur}, {DxfUtilities.Longueur(ligne):F3}, " +
                                        $"{DxfUtilities.Sens(ligne):F3}, " +
                                        $"\"{debutTexte}\", \"{finTexte}\", \"{longTexte}\", " +
                                        $"\"{ligne.AncienPlan?.Nom ?? ""}\", \"{ligne.NoEnt ?? ""}\"");

                                    ligne.M1 = ligne.M2;
                                }
                            }
                        }

                        // Vérifier les extrémités non trouvées
                        for (int i = 0; i < oldExtremite.Len; i++)
                        {
                            if (!oldExtremite[i].Trouve)
                            {
                                DxfUtilities.EcritErreur(hErr, hErr2,
                                    oldExtremite[i].Point.X,
                                    oldExtremite[i].Point.Y,
                                    oldExtremite[i].Point.Z,
                                    "Ce tronçon ne débouche sur rien.");
                                err = true;
                            }
                        }

                        oldExtremite = extremite;
                        extremite = new Liste<Extremite>();
                    }

                    // Vérifier les lignes non traitées
                    for (int i = 0; i < plan.Lignes.Len; i++)
                    {
                        Ligne ligne = plan.Lignes[i];
                        if (ligne.M1 != ligne.M2)
                        {
                            DxfUtilities.EcritErreur(hErr, hErr2,
                                (ligne.M1.X + ligne.M2.X) / 2,
                                (ligne.M1.Y + ligne.M2.Y) / 2,
                                (ligne.M1.Z + ligne.M2.Z) / 2,
                                "Ce tronçon n'a pas été traité.");
                            err = true;
                        }
                    }

                    // Vérifier les textes non traités
                    for (int i = 0; i < plan.Textes.Len; i++)
                    {
                        if (!string.IsNullOrEmpty(plan.Textes[i].TexteStr) &&
                            plan.Textes[i].TexteStr[0] != ' ')
                        {
                            if (plan.Textes[i].TexteStr[0] != '[')
                            {
                                DxfUtilities.EcritErreur(hErr, hErr2,
                                    plan.Textes[i].M.X,
                                    plan.Textes[i].M.Y,
                                    plan.Textes[i].M.Z,
                                    "Ce texte n'a pas été traité.");
                                err = true;
                            }
                            else
                            {
                                hFileTxt.WriteLine($"{plan.Textes[i].M.X:F2}, " +
                                    $"{plan.Textes[i].M.Y:F2}, " +
                                    $"{plan.Textes[i].M.Z:F2}, " +
                                    $"{plan.Textes[i].Couleur}, " +
                                    $"\"{plan.Textes[i].TexteStr}\"");
                            }
                        }
                    }

                    // Vérifier les blocs non traités
                    for (int i = 0; i < plan.Blocks.Len; i++)
                    {
                        if (!string.IsNullOrEmpty(plan.Blocks[i].Nom) &&
                            plan.Blocks[i].XDatas.Len > 0)
                        {
                            DxfUtilities.EcritErreur(hErr, hErr2,
                                plan.Blocks[i].M.X,
                                plan.Blocks[i].M.Y,
                                plan.Blocks[i].M.Z,
                                "Ce bloc n'a pas été traité.");
                            err = true;
                        }
                    }

                    DxfUtilities.EcritPiedErreur(hErr);
                    ascenceur?.Invoke(plan.Lignes.Len, plan.Lignes.Len + 1);

                    return err ? 1 : 0;
                }
            }
            catch (Exception)
            {
                return 2;
            }
        }
    }
}