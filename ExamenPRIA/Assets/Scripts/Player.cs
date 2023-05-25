using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class Player : NetworkBehaviour
    {
        //ID do equipo ao que pertence o xogador
        public NetworkVariable<int> teamID = new NetworkVariable<int>();
        //ID do material que ten o xogador actualmente
        public NetworkVariable<int> materialID = new NetworkVariable<int>();

        //Lista dos materiales dispoñibles
        public List<Material> materials = new List<Material>();

        //Variable que dicta si pode ou non moverse cando algún equipo está cheo
        public NetworkVariable<bool> canMove = new NetworkVariable<bool>();
        

        private MeshRenderer meshRenderer;
        private float speed = 40f;

        //Variable que dicta o tamaño máximo de un equipo
        public static int maxTeamMembers = 2;

        //Fago unha instancia do Player para facilitar a comunicación
        public static Player instance;

        private void Awake()
        {
            instance = this;
        }

        public override void OnNetworkSpawn()
        {
            //Iniciamos a teamId a -1 para que pinte o material por defecto cando cambie a 0
            if (IsOwner)
            {
                ChangeTeamIdServerRpc(-1);
                ChangeCanMoveServerRpc(true);
            }
            materialID.OnValueChanged += OnColorChanged;
            if (IsOwner)
            {
                //Nada mais spawnear si é propietario spawnearase nunha posición aleatoria do centro
                SpawnInCenter();
            }
            //En canto spawnea un xogador píntanse todos os xogadores
            PaintAllPlayers();

        }

        public void SpawnInCenter()
        {
            SetRandomStartingPositionServerRpc();
        }

        //Move ao xogador a unha posición aleatoria do centro do taboleiro e mira si ven de algún dos equipos para controlar a cantidade de xogadores que hai en cada un e restar 1 se é preciso
        //Despois asígnaselle o ID do equipo 0
        [ServerRpc]
        public void SetRandomStartingPositionServerRpc(ServerRpcParams rpcParams = default)
        {
            transform.position = RandomStartingPosition();
            if (teamID.Value == 2)
            {
                GameManager.instance.playersInTeam2.Value--;

            }
            else if (teamID.Value == 1)
            {
                GameManager.instance.playersInTeam1.Value--;

            }
            ChangeTeamIdServerRpc(0);
        }

        public static Vector3 RandomStartingPosition()
        {
            return new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-3f, 3f));
        }

        //Dalle ao cliente a ID do equipo ao que pertence e chamase a un método que se encarga de poñerlle o color axeitado
        [ServerRpc]
        void ChangeTeamIdServerRpc(int newTeamId, ServerRpcParams rpcParams = default)
        {
            teamID.Value = newTeamId;
            SetMaterialIdServerRpc();
            GameManager.instance.CheckTeamFull();
        }

        //Dalle ao cliente a ID do equipo ao que pertence e chamase a un método que se encarga de poñerlle o color axeitado
        [ServerRpc]
        void ChangeCanMoveServerRpc(bool canMove, ServerRpcParams rpcParams = default)
        {
            this.canMove.Value = canMove;
        }

        //Según a ID do equipo que teña e os materiales que estén ocupados asígnaráselle un ou outro
        [ServerRpc]
        void SetMaterialIdServerRpc(ServerRpcParams rpcParams = default)
        {
            GameManager.instance.UpdateTakenIdsList();
            if (teamID.Value == 0)
            {
                materialID.Value = 0;
            }
            else if (teamID.Value == 1)
            {
                do
                {
                    materialID.Value = Random.Range(1, 4);

                } while (GameManager.instance.takenMaterialIds.Contains(materialID.Value));
            }
            else 
            {
                do
                {
                    materialID.Value = Random.Range(4, materials.Count);

                } while (GameManager.instance.takenMaterialIds.Contains(materialID.Value));
            }
        }

        //Move ao xogador á posición que se lle indica desde o Update
        //Controla tamén cando entra na zona de cada equipo para asignarlle a ID correspondente
        //Chama ao GameManager cando esto último acontece para que leve a conta de cantos xogadores hai en cada equipo
        [ServerRpc]
        public void MoveADRequestServerRpc(Vector3 direction)
        {
            transform.position += (direction * speed * Time.deltaTime);
            if(transform.position.x > 10 && GameManager.instance.playersInTeam2.Value < maxTeamMembers)
            {
                if (teamID.Value != 2)
                {
                    GameManager.instance.playersInTeam2.Value++;
                    ChangeTeamIdServerRpc(2);
                }
                
            }
            else if(transform.position.x < -10 && GameManager.instance.playersInTeam1.Value < maxTeamMembers)
            {
                if (teamID.Value != 1)
                {
                    GameManager.instance.playersInTeam1.Value++;
                    ChangeTeamIdServerRpc(1);
                }

            }
            else if(transform.position.x > -10 && transform.position.x < 10)
            {
                if(teamID.Value == 2)
                {
                    GameManager.instance.playersInTeam2.Value--;

                }
                else if(teamID.Value == 1)
                {
                    GameManager.instance.playersInTeam1.Value--;

                }
                ChangeTeamIdServerRpc(0);
            }
        }

        

        public override void OnNetworkDespawn()
        {
            materialID.OnValueChanged -= OnColorChanged;
        }
        public void OnColorChanged(int previousValue, int newValue)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = materials[materialID.Value];
        }
        private void PaintAllPlayers()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = materials[materialID.Value];
        }

        [ClientRpc]
        public void SetCanMoveClientRpc(ClientRpcParams clientRpcParams = default)
        {
            foreach(ulong clientId in clientRpcParams.Send.TargetClientIds)
            {
                if(clientId == NetworkManager.Singleton.LocalClient.ClientId){
                    ChangeCanMoveServerRpc(false);
                }
                else
                {
                    ChangeCanMoveServerRpc(true);
                }
            }

            
        }
        //Non hai moito que explicar aquí, detecta as teclas de movemento sempre que non haxa un equipo cheo ou seas de ese equipo si está cheo
        //E ao final dille a cada un de que color debe ser e actualiza a lista dos colores que están collidos
        void Update()
        {
            if (IsOwner)
            {
                if (canMove.Value)
                {
                    if (Input.GetKey(KeyCode.D))
                    {
                        MoveADRequestServerRpc(Vector3.right);
                    }
                    if (Input.GetKey(KeyCode.A))
                    {
                        MoveADRequestServerRpc(-Vector3.right);
                    }
                    if (Input.GetKey(KeyCode.W))
                    {
                        MoveADRequestServerRpc(Vector3.forward);
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        MoveADRequestServerRpc(-Vector3.forward);
                    }
                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        SpawnInCenter();
                    }
                }
            }
        }
    }
}