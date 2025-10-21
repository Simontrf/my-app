using System;
using DxfLibrary;

public class Program
{
    // Variable statique pour Ascenseur (remplace le static local de C++)
    private static int ascenseurProgress = 0;

    static void Ascenseur(long Tot, long Count)
    {
        int i;
        if (Count == 0)
        {
            Console.WriteLine("\t <0                  100%>");
            Console.Write("\t ");
            ascenseurProgress = 0;
        }
        else
        {
            if (Count > Tot)
            {
                while (ascenseurProgress < 25)
                {
                    Console.Write("█");  // Caractère de bloc (ou "±" si préféré)
                    ascenseurProgress++;
                }
                Console.WriteLine();
                Console.WriteLine("Fini");
            }
            else
            {
                if (Tot > 25)
                {
                    if (Count / (Tot / (float)25) > ascenseurProgress)
                    {
                        Console.Write("█");
                        ascenseurProgress++;
                    }
                }
                else
                {
                    Console.Write("█");
                    ascenseurProgress++;
                }
            }
        }
    }

    static void Ascenseur1(long Tot, long Count)
    {
        if (Count == 0)
        {
            Console.WriteLine($"Taille: {Tot} Octets");
            Console.WriteLine("Lecture du fichier: ");
        }
        Ascenseur(Tot, Count);
    }

    static void Ascenseur2(long Tot, long Count)
    {
        if (Count == 0)
        {
            Console.WriteLine($"{Tot} Tronçons trouvés");
            Console.WriteLine("Traitement des tronçons");
        }
        Ascenseur(Tot, Count);
    }

    static void Main(string[] args)
    {
        string Fichier;
        string Plan;
        Dessin DessinDxf = new Dessin();  // Instanciation correcte
        int i, ii;

        Console.Clear();

        if (args.Length > 0)
        {
            Fichier = args[0];
        }
        else
        {
            Console.Write("Entrez le nom du fichier DXF: ");
            Fichier = Console.ReadLine();
        }

        ii = Fichier.Length;

        for (i = 0; i < ii; i++)
        {
            if (Fichier[i] == '.')
            {
                ii = -1;
                break;
            }
        }
        
        if (ii != -1)
        {
            Fichier += ".dxf";
        }

        Console.WriteLine($"Fichier : {Fichier}");

        // Lecture du fichier DXF
        if (!DessinDxf.LitDXF(Ascenseur1, Fichier))
        {
            Console.WriteLine("Fichier non trouvé.");
            Environment.Exit(1);
        }

        // Sélection du plan
        if (args.Length > 1)
        {
            Plan = args[1];  // Correction: args[1] au lieu de args[2]
        }
        else
        {
            Console.WriteLine("\nListe des plans:");
            for (i = 0; i < DessinDxf.Plans.Len; i++)
            {
                Console.WriteLine($"  {DessinDxf.Plans[i].Nom}");
            }
            Console.Write("\nEntrez le nom du plan: ");
            Plan = Console.ReadLine();
        }

        Console.WriteLine($"Plan: {Plan}");

        // Traitement et écriture
        i = DessinDxf.EcrisTroncon(Ascenseur2, "Dxf.out", Plan, false);
        Console.WriteLine();

        switch (i)
        {
            case 1:
                Console.WriteLine("ATTENTION: Il y a des erreurs de trace dans le fichier DXF.");
                Console.WriteLine("Pressez une touche pour continuer...");
                Console.ReadKey();
                Environment.Exit(3);
                break;

            case 2:
                Console.WriteLine("ATTENTION: Ne peut créer le fichier de sortie.");
                Console.WriteLine("Pressez une touche pour continuer...");
                Console.ReadKey();
                Environment.Exit(2);
                break;

            case 3:
                Console.WriteLine("L'importation s'est déroulée avec succès.");
                Console.WriteLine("Pressez une touche pour continuer...");
                Console.ReadKey();
                break;

            default:
                Console.WriteLine($"Code de retour inconnu: {i}");
                break;
        }
    }
}