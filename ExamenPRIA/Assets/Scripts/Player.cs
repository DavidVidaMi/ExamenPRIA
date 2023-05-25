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
        

        private MeshRenderer meshRenderer;
        private float speed = 40f;

        //ID do equipo que está cheo, 0 por defecto cando non hai ningún
        private int fullTeamID = 0;

        //Variable que dicta o tamaño máximo de un equipo
        public int maxTeamMembers = 2;

        public override void OnNetworkSpawn()
        {
            //Iniciamos a teamId a -1 para que pinte o material por defecto cando cambie a 0
            if (IsOwner)
            {
                ChangeTeamIdServerRpc(-1);
            }
            materialID.OnValueChanged += OnColorChanged;
            if (IsOwner)
            {
                //Nada mais spawnear si é propietario spawnearase nunha posición aleatoria do centro
                SpawnInCenter();
            }

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

        //Mira si hai algún equipo cheo, devolve true si é o caso e cambia a fullTeamID en consecuencia, se non devolve False e seteaa a 0
        private bool TeamFull()
        {
            if(GameManager.instance.playersInTeam1.Value >= maxTeamMembers)
            {
                fullTeamID = 1;
                return true;
            }
            else if (GameManager.instance.playersInTeam2.Value >= maxTeamMembers)
            {
                fullTeamID = 2;
                return true;
            }
            else
            {
                fullTeamID = 0;
                return false;
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
        //Non hai moito que explicar aquí, detecta as teclas de movemento sempre que non haxa un equipo cheo ou seas de ese equipo si está cheo
        //E ao final dille a cada un de que color debe ser e actualiza a lista dos colores que están collidos
        void Update()
        {
            if (IsOwner)
            {
                if (!TeamFull() || teamID.Value == fullTeamID)
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