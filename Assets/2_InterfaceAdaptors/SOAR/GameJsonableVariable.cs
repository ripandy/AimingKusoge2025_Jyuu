using Domain;
using Soar.Variables;
using UnityEngine;

namespace Kusoge.SOAR
{
    [CreateAssetMenu(fileName = "GameJsonableVariable", menuName = "Kusoge/GameJsonableVariable", order = -1)]
    public class GameJsonableVariable : JsonableVariable<Game>
    {
    }
}