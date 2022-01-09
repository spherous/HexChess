using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Player 
{
    public string name;
    public Team team;   
    public bool isHost;
    
    public Player(string name, Team team, bool isHost)
    {
        this.name = name;
        this.team = team;
        this.isHost = isHost;        
    }
}