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

        //Lista dos materiales dispo�ibles
        public List<Material> materials = new List<Material>();
        //Lista dos IDs dos materiales que xa est�n asignados
        public List<int> takenMaterialIds = new List<int>();

        private MeshRenderer meshRenderer;
        private float speed = 40f;

        //ID do equipo que est� cheo, 0 por defecto cando non hai ning�n
        private int fullTeamID = 0;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                //Nada mais spawnear si � propietario spawnearase nunha posici�n aleatoria do centro
                SpawnInCenter();
            }

        }

        public void SpawnInCenter()
        {
            SetRandomStartingPositionServerRpc();
        }

        //Move ao xogador a unha posici�n aleatoria do centro do taboleiro e mira si ven de alg�n dos equipos para controlar a cantidade de xogadores que hai en cada un e restar 1 se � preciso
        //Despois as�gnaselle o ID do equipo 0
        [ServerRpc]
        void SetRandomStartingPositionServerRpc(ServerRpcParams rpcParams = default)
        {
            transform.position = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-3f, 3f));
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

        //Dalle ao cliente a ID do equipo ao que pertence e chamase a un m�todo que se encarga de po�erlle o color axeitado
        [ServerRpc]
        void ChangeTeamIdServerRpc(int newTeamId, ServerRpcParams rpcParams = default)
        {
            teamID.Value = newTeamId;
            SetMaterialIdServerRpc();
        }

        //Seg�n a ID do equipo que te�a e os materiales que est�n ocupados as�gnar�selle un ou outro
        [ServerRpc]
        void SetMaterialIdServerRpc(ServerRpcParams rpcParams = default)
        {
            if(teamID.Value == 0)
            {
                materialID.Value = 0;
            }
            else if (teamID.Value == 1)
            {
                do
                {
                    materialID.Value = Random.Range(1, 4);

                } while (takenMaterialIds.Contains(materialID.Value));
            }
            else 
            {
                do
                {
                    materialID.Value = Random.Range(4, materials.Count);

                } while (takenMaterialIds.Contains(materialID.Value));
            }
        }

        //Move ao xogador � posici�n que se lle indica desde o Update
        //Controla tam�n cando entra na zona de cada equipo para asignarlle a ID correspondente
        //Chama ao GameManager cando esto �ltimo acontece para que leve a conta de cantos xogadores hai en cada equipo
        [ServerRpc]
        public void MoveADRequestServerRpc(Vector3 direction)
        {
            transform.position += (direction * speed * Time.deltaTime);
            if(transform.position.x > 10 && GameManager.instance.playersInTeam2.Value < 2)
            {
                if (teamID.Value != 2)
                {
                    GameManager.instance.playersInTeam2.Value++;
                    ChangeTeamIdServerRpc(2);
                }
                
            }
            else if(transform.position.x < -10 && GameManager.instance.playersInTeam1.Value < 2)
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

        //Mira si hai alg�n equipo cheo, devolve true si � o caso e cambia a fullTeamID en consecuencia, se non devolve False e seteaa a 0
        private bool TeamFull()
        {
            if(GameManager.instance.playersInTeam1.Value >= 2)
            {
                fullTeamID = 1;
                return true;
            }
            else if (GameManager.instance.playersInTeam2.Value >= 2)
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

        //Este m�todo enc�rgase de actualizar a lista dos IDs materiales que est�n collidos
        //S�, esto deber�a chamarse desde un OnValueChange... non te�o moi claro como se fai e non me vou a liar con eso ahora, perd�n :c
        public void UpdateTakenIdsList()
        {
            takenMaterialIds = new List<int>();
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
            {
                takenMaterialIds.Add(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().materialID.Value);
            }
        }
        //Non hai moito que explicar aqu�, detecta as teclas de movemento sempre que non haxa un equipo cheo ou seas de ese equipo si est� cheo
        //E ao final dille a cada un de que color debe ser e actualiza a lista dos colores que est�n collidos
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
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material = materials[materialID.Value];
            UpdateTakenIdsList();
        }
    }
}