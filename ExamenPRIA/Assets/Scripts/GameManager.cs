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
        //Lista dos IDs dos materiales que xa están asignados
        public List<int> takenMaterialIds = new List<int>();

        //ID do equipo que está cheo, 0 por defecto cando non hai ningún
        public NetworkVariable<int> fullTeamID = new NetworkVariable<int>();

        private void Start()
        {
            fullTeamID.Value = 0;
        }

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
        static void StartButtons()
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }

        //Dicta si eres host ou cliente
        static void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ?
                "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }

        //Si pulsas o botón de moverte ao inicio móvete ao inicio collendo o player que o pulsou e chamando ao método SpawnInCenter
        static void MoveToStart()
        {
            if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Mover todos ao inicio" : "Moverse ao inicio"))
            {
                if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
                {
                    foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                    {
                        //Modificamos manualmente a posición de todos os players na lista porque chamando a serverRPC non parece funcionar
                        NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().transform.position = Player.RandomStartingPosition();
                    }
                }
                else
                {
                    var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                    var player = playerObject.GetComponent<Player>();
                    player.SpawnInCenter();
                }
            }
        }

        //Este método encárgase de actualizar a lista dos IDs materiales que están collidos
        //Sí, esto debería chamarse desde un OnValueChange... non teño moi claro como se fai e non me vou a liar con eso ahora, perdón :c
        public void UpdateTakenIdsList()
        {
            takenMaterialIds = new List<int>();
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
            {
                takenMaterialIds.Add(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().materialID.Value);
            }
        }

        //Mira si hai algún equipo cheo, devolve true si é o caso e cambia a fullTeamID en consecuencia, se non devolve False e seteaa a 0
        public void CheckTeamFull()
        {
            if (GameManager.instance.playersInTeam1.Value >= Player.maxTeamMembers)
            {
                fullTeamID.Value = 1;
            }
            else if (GameManager.instance.playersInTeam2.Value >= Player.maxTeamMembers)
            {
                fullTeamID.Value = 2;
            }
            else
            {
                fullTeamID.Value = 0;
            }
            ControlPlayerMovements();
        }

        private void ControlPlayerMovements()
        {
            List<ulong> playersThatCantMove = new List<ulong>();
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if(fullTeamID.Value != 0)
                {
                    if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().teamID.Value != fullTeamID.Value)
                    {
                        playersThatCantMove.Add(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).OwnerClientId);
                    }
                }
            }
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams {TargetClientIds = playersThatCantMove}
            };

            Debug.Log("Lista que manda o server"+playersThatCantMove.Count);
            Player.instance.SetCanMoveClientRpc(clientRpcParams);
        }
    }
}