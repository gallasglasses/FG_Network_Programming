#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

[CustomEditor(typeof(GameLobby))]
public class GameLobbyEditor : Editor
{
    private string lobbyCodeInput = "";
    private string lobbyNameInput = "";
    private bool isLobbyPrivate = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameLobby gameLobby = (GameLobby)target;

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("This editor will be showed in edit mode.", MessageType.Info);
            return;
        }

        if (gameLobby == null)
        {
            EditorGUILayout.HelpBox("GameLobby is not initialized.", MessageType.Warning);
            return;
        }

        lobbyNameInput = EditorGUILayout.TextField("Lobby Name", lobbyNameInput);
        if (string.IsNullOrEmpty(lobbyNameInput))
        {
            EditorGUILayout.HelpBox("Lobby Name cannot be empty. Please fill it in.", MessageType.Warning);
        }
        isLobbyPrivate = EditorGUILayout.Toggle("Is Lobby Private", isLobbyPrivate);
        if (GUILayout.Button("Create Lobby"))
        {
            if (!string.IsNullOrWhiteSpace(lobbyNameInput))
            {
                gameLobby.CreateLobby(lobbyNameInput, isLobbyPrivate);
            }
            else
            {
                Debug.LogWarning("Lobby Name cannot be empty!");
            }
        }

        EditorGUILayout.LabelField("Join Lobby By Code", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        lobbyCodeInput = EditorGUILayout.TextField("Lobby Code", lobbyCodeInput);

        if (GUILayout.Button("Join Lobby By Code"))
        {
            if (!string.IsNullOrWhiteSpace(lobbyCodeInput))
            {
                gameLobby.JoinLobbyByCode(lobbyCodeInput);
            }
            else
            {
                Debug.LogWarning("Lobby code cannot be empty!");
            }
        }
        EditorGUILayout.EndHorizontal();
        if (string.IsNullOrEmpty(lobbyCodeInput))
        {
            EditorGUILayout.HelpBox("Lobby Code cannot be empty. Please fill it in.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Join Lobby By Id"))
        {
            gameLobby.Invoke("JoinLobbyById", 0f);
        }

        if (GUILayout.Button("Quick Join Lobby"))
        {
            gameLobby.Invoke("QuickJoinLobby", 0f);
        }

        if (GUILayout.Button("Leave Lobby"))
        {
            gameLobby.Invoke("LeaveLobby", 0f);
        }

        if (GUILayout.Button("List Lobbies"))
        {
            gameLobby.Invoke("ListLobbies", 0f);
        }

        if (gameLobby.IsHostPlayer(AuthenticationService.Instance.PlayerId))
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Lobby"))
            {
                gameLobby.Invoke("DeleteLobby", 0f);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lobby Players", EditorStyles.boldLabel);

            List<Player> players = gameLobby.GetLobbyPlayers();
            if (players.Count > 0)
            {
                foreach (Player player in players)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"ID: {player.Id}");

                    if(!gameLobby.IsHostPlayer(player.Id))
                    {
                        if (GUILayout.Button("Kick"))
                        {
                            gameLobby.KickPlayer(player.Id);
                        }
                        if (GUILayout.Button("SetAsHost"))
                        {
                            gameLobby.MigrateLobbyHost(player.Id);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No players in the lobby.");
            }
        }
        else
        {
            EditorGUILayout.LabelField("You are not the host. Only the host can manage players.");
        }

    }
}
#endif