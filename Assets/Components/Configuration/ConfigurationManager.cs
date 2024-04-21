using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigurationManager : Singleton<ConfigurationManager>
{

    /// <summary>
    /// The seed for world generation.
    /// </summary>
    public int Seed = 1337;

    /// <summary>
    /// The number of chunks in the x and z dimension of the world.
    /// </summary>
    public int World_Diameter = 16;

    /// <summary>
    /// The number of chunks in the y dimension of the world.
    /// </summary>
    public int World_Height = 4;

    /// <summary>
    /// The number of blocks in any dimension of a chunk.
    /// </summary>
    public int Chunk_Diameter = 8;

    /// <summary>
    /// How much of the tile map does each tile take up.
    /// </summary>
    public float Tile_Map_Unit_Ratio = 0.25f;

    /// <summary>
    /// The number of acidic regions on the map.
    /// </summary>
    public int Number_Of_Acidic_Regions = 10;

    /// <summary>
    /// The radius of each acidic region
    /// </summary>
    public int Acidic_Region_Radius = 5;

    /// <summary>
    /// The number of acidic regions on the map.
    /// </summary>
    public int Number_Of_Conatiner_Spheres = 5;

    /// <summary>
    /// The radius of each acidic region
    /// </summary>
    public int Conatiner_Sphere_Radius = 20;

    //Begin SAM variables
    /// <summary>
    /// How many ants will run in each simulation instance
    /// </summary>
    [Range(1, 500)]
    public int Number_of_ants = 10;

    /// <summary>
    /// How much max health the ants can have
    /// </summary>
    public int Max_ant_health = 100;

    /// <summary>
    /// After each simulation run how many of the top performers will be allowed to mutate and continue
    /// </summary>
    public int Selection_pool_size = 2;

    /// <summary>
    /// After selection how many children will each selected simulation produce for the next round? Parents are automatically included using a + strategy
    /// </summary>
    public int Selected_offspring_count = 2;

    public float dt = 0.5f;

    /// <summary>
    ///Disables all graphical things such as chunk remeshing. Massively increases simulation speed!!
    /// </summary>
    public bool SimulationOnly = false;
}
