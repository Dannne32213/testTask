#include <nn/fs.h>
#include <nn/account.h>
#include <map>
#include <string>
#include <vector>
#include <cstdio>
#include <cstring>

#define EXPORT_API extern "C"
#define SWITCH_LOG(...) printf("[SwitchPrefs_Native] " __VA_ARGS__); printf("\n")

std::map<std::string, int> g_IntPrefs;
std::map<std::string, std::string> g_StringPrefs;
std::map<std::string, float> g_FloatPrefs;

bool g_IsInitialized = false;
nn::account::UserHandle g_UserHandle;
const char* g_MountName = "save";
const char* g_FullSavePath = "save:/SwitchPrefs.dat";

bool DoesFileExist(const char* p) {
    nn::fs::DirectoryEntryType t;
    return nn::fs::GetEntryType(&t, p).IsSuccess();
}

void Internal_SaveToDisk();

void UniversalHeuristicExtractor(const char* data, int64_t size) {
    SWITCH_LOG(">>> [UNIVERSAL EXTRACTOR] Incepere scanare...");
    std::string currentWord = "";

    for (int64_t i = 0; i < size; i++) {
        char c = data[i];
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') {
            currentWord += c;
        }
        else {
            int wLen = currentWord.length();
            if (wLen >= 3 && wLen <= 64) {
                if (currentWord != "1NVS" && currentWord != "DAT" && currentWord != "save" && currentWord != "list") {
                    bool foundValue = false;
                    size_t searchStart = i;
                    size_t searchEnd = i + 6 > size ? size : i + 6;

                    for (size_t j = searchStart; j < searchEnd; j++) {
                        // 1. CAUTĂ JSON-URI
                        if (data[j] == '{') {
                            std::string json; int braces = 0; size_t k = j;
                            while (k < size) {
                                if (data[k] == '{') braces++;
                                if (data[k] == '}') braces--;
                                json += data[k];
                                k++;
                                if (braces == 0) break;
                            }
                            if (json.length() > 2 && braces == 0) {
                                g_StringPrefs[currentWord] = json;
                                SWITCH_LOG(">>> [AUTO] JSON: %s", currentWord.c_str());
                                foundValue = true;
                                i = k - 1;
                                break;
                            }
                        }

                        // 2. CAUTĂ LISTE
                        if (!foundValue && ((data[j] >= '0' && data[j] <= '9') || data[j] == '-')) {
                            std::string listStr = ""; size_t k = j; bool hasComma = false;
                            while (k < size && ((data[k] >= '0' && data[k] <= '9') || data[k] == '.' || data[k] == ',' || data[k] == '-' || data[k] == ' ')) {
                                if (data[k] == ',') hasComma = true;
                                listStr += data[k];
                                k++;
                            }
                            if (hasComma && listStr.length() >= 3) {
                                g_StringPrefs[currentWord] = listStr;
                                SWITCH_LOG(">>> [AUTO] LISTA: %s", currentWord.c_str());
                                foundValue = true;
                                i = k - 1;
                                break;
                            }
                        }
                    }

                    // 3. VÂNĂTORUL DE TEXTE (Asta îți va da "LEVEL 0" curat!)
                    if (!foundValue && (currentWord == "LastScene" || currentWord == "SavedScene" || currentWord == "PlayerName" || currentWord == "SavedLevel")) {
                        std::string extractedStr = "";
                        size_t sStart = i + 1;
                        size_t sMax = sStart + 10; // Caută începutul textului în următorii 10 octeți

                        for (size_t s = sStart; s < sMax && s < size; s++) {
                            // Dacă dăm de prima literă sau cifră, începem să citim
                            if ((data[s] >= 'A' && data[s] <= 'Z') || (data[s] >= 'a' && data[s] <= 'z') || (data[s] >= '0' && data[s] <= '9')) {
                                size_t k = s;
                                // Citim doar caractere vizibile ASCII (inclusiv Spațiul = codul 32)
                                while (k < size && data[k] >= 32 && data[k] <= 126) {
                                    extractedStr += data[k];
                                    k++;
                                }
                                if (extractedStr.length() > 0) {
                                    g_StringPrefs[currentWord] = extractedStr;
                                    SWITCH_LOG(">>> [AUTO EXACT] STRING: %s = %s", currentWord.c_str(), extractedStr.c_str());
                                    foundValue = true;
                                    i = k - 1;
                                }
                                break; // Ieșim din căutare imediat ce am găsit textul
                            }
                        }
                    }

                    // 4. FALLBACK: INT sau FLOAT
                    if (!foundValue && i + 4 <= size) {
                        int valI = 0; memcpy(&valI, data + i + 1, 4);
                        float valF = 0.0f; memcpy(&valF, data + i + 1, 4);

                        if (g_IntPrefs.count(currentWord) == 0) g_IntPrefs[currentWord] = valI;
                        if (g_FloatPrefs.count(currentWord) == 0) g_FloatPrefs[currentWord] = valF;

                        if (valI == 0) { SWITCH_LOG(">>> [AUTO EXACT] ZERO: %s = 0", currentWord.c_str()); }
                        else if (valF >= -99999.0f && valF <= 999999.0f && valF != (float)valI) { SWITCH_LOG(">>> [AUTO EXACT] FLOAT: %s = %f", currentWord.c_str(), valF); }
                        else { SWITCH_LOG(">>> [AUTO EXACT] INT: %s = %d", currentWord.c_str(), valI); }

                        i += 4;
                    }
                }
            }
            currentWord = "";
        }
    }
    SWITCH_LOG(">>> [UNIVERSAL EXTRACTOR] Scanare finalizata cu succes!");
}

void Internal_LoadFromDisk() {
    SWITCH_LOG(">>> INIT SEQUENCE STARTED...");

    // 1. Daca fisierul a fost ascuns in testele tale anterioare, ii dam inapoi numele ca sa il citeasca Unity.
    if (DoesFileExist("save:/PlayerPrefs_MIGRATED.dat")) {
        nn::fs::RenameFile("save:/PlayerPrefs_MIGRATED.dat", "save:/PlayerPrefs.dat");
        nn::fs::CommitSaveData(g_MountName);
    }

    // 2. Incarcam salvarea de V1
    if (DoesFileExist(g_FullSavePath)) {
        SWITCH_LOG(">>> Found V1 Save (%s). Loading...", g_FullSavePath);
        nn::fs::FileHandle h;
        if (nn::fs::OpenFile(&h, g_FullSavePath, nn::fs::OpenMode_Read).IsSuccess()) {
            int64_t s; nn::fs::GetFileSize(&s, h);
            if (s >= 4) {
                char sig[4]; nn::fs::ReadFile(h, 0, sig, 4);
                if (memcmp(sig, "1NVS", 4) != 0) {
                    int64_t o = 0; int cnt = 0;
                    if (nn::fs::ReadFile(h, o, &cnt, 4).IsSuccess()) {
                        o += 4;
                        for (int i = 0; i < cnt; i++) {
                            int kL; nn::fs::ReadFile(h, o, &kL, 4); o += 4;
                            std::vector<char> k(kL + 1, 0); nn::fs::ReadFile(h, o, k.data(), kL); o += kL;
                            int v; nn::fs::ReadFile(h, o, &v, 4); o += 4;
                            g_IntPrefs[std::string(k.data(), kL)] = v;
                        }
                    }
                    if (nn::fs::ReadFile(h, o, &cnt, 4).IsSuccess()) {
                        o += 4;
                        for (int i = 0; i < cnt; i++) {
                            int kL; nn::fs::ReadFile(h, o, &kL, 4); o += 4;
                            std::vector<char> k(kL + 1, 0); nn::fs::ReadFile(h, o, k.data(), kL); o += kL;
                            int vL; nn::fs::ReadFile(h, o, &vL, 4); o += 4;
                            std::vector<char> v(vL + 1, 0); nn::fs::ReadFile(h, o, v.data(), vL); o += vL;
                            g_StringPrefs[std::string(k.data(), kL)] = std::string(v.data(), vL);
                        }
                    }
                    if (nn::fs::ReadFile(h, o, &cnt, 4).IsSuccess()) {
                        o += 4;
                        for (int i = 0; i < cnt; i++) {
                            int kL; nn::fs::ReadFile(h, o, &kL, 4); o += 4;
                            std::vector<char> k(kL + 1, 0); nn::fs::ReadFile(h, o, k.data(), kL); o += kL;
                            float v; nn::fs::ReadFile(h, o, &v, 4); o += 4;
                            g_FloatPrefs[std::string(k.data(), kL)] = v;
                        }
                    }
                }
            }
            nn::fs::CloseFile(h);
        }
    }

    // 3. Rulam extractorul ca sa printeze Log-urile tale, dar DOAR o singura data
    if (DoesFileExist("save:/PlayerPrefs.dat") && !DoesFileExist("save:/LOGS_DONE.flag")) {
        SWITCH_LOG(">>> Found Legacy Save. Extracting...");
        nn::fs::FileHandle h;
        if (nn::fs::OpenFile(&h, "save:/PlayerPrefs.dat", nn::fs::OpenMode_Read).IsSuccess()) {
            int64_t s; nn::fs::GetFileSize(&s, h);
            if (s > 4) {
                std::vector<char> b(s + 1, 0);
                nn::fs::ReadFile(h, 0, b.data(), s);
                UniversalHeuristicExtractor(b.data(), s);
            }
            nn::fs::CloseFile(h);
        }

        // IMPORTANT: Aici salvam un flag gol. NU mai ascundem PlayerPrefs.dat cum faceam inainte!
        nn::fs::CreateFile("save:/LOGS_DONE.flag", 1);
        Internal_SaveToDisk();
    }
}

void Internal_SaveToDisk() {
    const char* tempSavePath = "save:/SwitchPrefs_temp.dat";
    if (DoesFileExist(tempSavePath)) nn::fs::DeleteFile(tempSavePath);

    int64_t ts = 12;
    for (auto const& p : g_IntPrefs) ts += 8 + p.first.length();
    for (auto const& p : g_StringPrefs) ts += 8 + p.first.length() + p.second.length();
    for (auto const& p : g_FloatPrefs) ts += 8 + p.first.length();

    std::vector<char> buffer(ts, 0);
    int64_t o = 0;

    int iC = (int)g_IntPrefs.size();
    memcpy(buffer.data() + o, &iC, 4); o += 4;
    for (auto const& p : g_IntPrefs) {
        int kL = p.first.length();
        memcpy(buffer.data() + o, &kL, 4); o += 4;
        memcpy(buffer.data() + o, p.first.c_str(), kL); o += kL;
        memcpy(buffer.data() + o, &p.second, 4); o += 4;
    }

    int sC = (int)g_StringPrefs.size();
    memcpy(buffer.data() + o, &sC, 4); o += 4;
    for (auto const& p : g_StringPrefs) {
        int kL = p.first.length();
        memcpy(buffer.data() + o, &kL, 4); o += 4;
        memcpy(buffer.data() + o, p.first.c_str(), kL); o += kL;
        int vL = p.second.length();
        memcpy(buffer.data() + o, &vL, 4); o += 4;
        memcpy(buffer.data() + o, p.second.c_str(), vL); o += vL;
    }

    int fC = (int)g_FloatPrefs.size();
    memcpy(buffer.data() + o, &fC, 4); o += 4;
    for (auto const& p : g_FloatPrefs) {
        int kL = p.first.length();
        memcpy(buffer.data() + o, &kL, 4); o += 4;
        memcpy(buffer.data() + o, p.first.c_str(), kL); o += kL;
        memcpy(buffer.data() + o, &p.second, 4); o += 4;
    }

    nn::fs::CreateFile(tempSavePath, ts);
    nn::fs::FileHandle h;
    if (nn::fs::OpenFile(&h, tempSavePath, nn::fs::OpenMode_Write).IsSuccess()) {
        nn::fs::WriteOption opt; opt.flags = nn::fs::WriteOptionFlag_Flush;
        nn::fs::WriteFile(h, 0, buffer.data(), ts, opt);
        nn::fs::CloseFile(h);

        if (DoesFileExist(g_FullSavePath)) nn::fs::DeleteFile(g_FullSavePath);
        nn::fs::RenameFile(tempSavePath, g_FullSavePath);

        nn::fs::CommitSaveData(g_MountName);
        SWITCH_LOG("Data committed atomically. Size: %lld bytes", (long long)ts);
    }
}

extern "C" {
    EXPORT_API bool Native_SwitchPrefs_Init() {
        if (g_IsInitialized) return true;
        nn::account::Initialize();
        if (nn::account::TryOpenPreselectedUser(&g_UserHandle)) {
            nn::account::Uid uid; nn::account::GetUserId(&uid, g_UserHandle);
            if (nn::fs::MountSaveData(g_MountName, uid).IsSuccess()) {
                g_IsInitialized = true; Internal_LoadFromDisk(); return true;
            }
        } return false;
    }
    EXPORT_API void Native_SwitchPrefs_Save() { if (g_IsInitialized) Internal_SaveToDisk(); }
    EXPORT_API void Native_SwitchPrefs_SetInt(const char* k, int v) { if (k) { g_IntPrefs[k] = v; } }
    EXPORT_API int Native_SwitchPrefs_GetInt(const char* k, int d) { return (k && g_IntPrefs.count(k)) ? g_IntPrefs[k] : d; }
    EXPORT_API void Native_SwitchPrefs_SetFloat(const char* k, float v) { if (k) { g_FloatPrefs[k] = v; } }
    EXPORT_API float Native_SwitchPrefs_GetFloat(const char* k, float d) { return (k && g_FloatPrefs.count(k)) ? g_FloatPrefs[k] : d; }
    EXPORT_API bool Native_SwitchPrefs_HasKey(const char* k) {
        if (!k) return false;
        std::string s(k);
        return g_IntPrefs.count(s) || g_StringPrefs.count(s) || g_FloatPrefs.count(s);
    }
    EXPORT_API void Native_SwitchPrefs_DeleteKey(const char* k) { if (k) { std::string s(k); g_IntPrefs.erase(s); g_StringPrefs.erase(s); g_FloatPrefs.erase(s); } }
    EXPORT_API void Native_SwitchPrefs_DeleteAll() { g_IntPrefs.clear(); g_StringPrefs.clear(); g_FloatPrefs.clear(); }

    EXPORT_API int Native_SwitchPrefs_GetStringLength(const char* k) {
        return (k && g_StringPrefs.count(k)) ? (int)g_StringPrefs[k].length() : 0;
    }

    EXPORT_API void Native_SwitchPrefs_GetStringBuffer(const char* k, char* buffer) {
        if (k && buffer && g_StringPrefs.count(k)) {
            memcpy(buffer, g_StringPrefs[k].c_str(), g_StringPrefs[k].length());
        }
    }

    EXPORT_API void Native_SwitchPrefs_SetStringBuffer(const char* k, const char* buffer, int length) {
        if (k && buffer) {
            g_StringPrefs[k] = std::string(buffer, length);
        }
    }

    EXPORT_API void Native_SwitchPrefs_GetUserHandle(nn::account::UserHandle* outHandle) {
        if (outHandle && g_IsInitialized) {
            *outHandle = g_UserHandle;
        }
    }
}