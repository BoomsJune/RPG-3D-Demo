using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public CharacterStates playerStates;

    List<IEndGameObserver> endGameObservers = new List<IEndGameObserver>();

    public void RegisterPlayer(CharacterStates player)
    {
        playerStates = player;
    }

    public void AddObserver(IEndGameObserver observer)
    {
        endGameObservers.Add(observer);
    }
   
    public void RemoveObserver(IEndGameObserver observer)
    {
        endGameObservers.Remove(observer);
    }

    public void NotifyObservers()
    {
        foreach(var observer in endGameObservers)
        {
            Debug.Log("notify: "+ observer.ToString());
            observer.EndNotify();
        }
    }
}
