using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomDebug
{

    public static void Log(string message)
    {
        // if (URLImageRetriever.instance.testMode)
        {
            Debug.Log(message);
        }
    }
    
    public static void LogError(string message)
    {
        // if (URLImageRetriever.instance.testMode)
        {
            Debug.LogError(message);
        }
    }
        
   
}
