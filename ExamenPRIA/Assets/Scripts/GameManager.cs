using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class GameManager : NetworkBehaviour
    {
        //Fago unha instancia do GameManager para facilitar a comunicación
        public static GameManager instance;

        //Variables para levar a conta dos xogadores que hai en cada equipo
        public NetworkVariable<int> playersInTeam1 = new NetworkVariable<int>();
        public NetworkVariable<int> playersInTeam2 = new NetworkVariable<int>();

        //Monta a GUI
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();

                MoveToStart();
            }

            GUILayout.EndArea();
        }

        //Crea a instancia
        private void Awake()
        {
            instance = this;
        }

        //Mostra os botóns de si te queres conectar como Host ou como Cliente e conéctache como host ou cliente
        void StartButtons()
        {
            if (GUILayout.Button("Host"))
            {
                NetworkManager.Singleton.StartHost();
            }
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        }

        //Dicta si eres host ou cliente
        static void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }

        //Si pulsas o botón de moverte ao inicio móvete ao inicio collendo o player que o pulsou e chamando ao método SpawnInCenter
        static void MoveToStart()
        {
            if (GUILayout.Button("Mover a inicio"))
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<Player>();
                player.SpawnInCenter();
            }
        }
    }
}