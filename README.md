## Hurtworld Legacy Battle Royal Automatic Event

Plugin do gry Hurtworld Legacy dodający automatyczny event typu Battle Royale.
Gracze mogą zapisywać się do kolejki, po czym zostają teleportowani na arenę, gdzie walczą aż do wyłonienia zwycięzcy.


## ⚠️ Ważna informacja

Plugin został napisany z myślą o serwerowni hw69.pl, gdzie używana jest specjalnie przygotowana arena.
Jeśli nie posiadasz tej mapy:
- gracze po starcie eventu mogą zostać przeteleportowani w powietrze
- wymagane jest dostosowanie *ChestSpawnPoints* oraz spawnów graczy


## 📜 Licencja

- Plugin został stworzony dla serwisu **hw69.pl**, na którym aktualnie działa
- Dozwolone jest jego używanie na innych serwerach
- Modyfikacje są dozwolone
- Możesz dostosować plugin pod własne potrzeby (mapa, spawn pointy, loot itd.)

❗ W przypadku używania poza oryginalnym środowiskiem (hw69.pl), wymagane jest dostosowanie areny — w przeciwnym razie teleport może przenieść graczy w powietrze

❗ Zabrania się usuwania informacji o autorze pluginu

❗ Zabrania się podpisywania się pod pluginem jako jego twórca bez zgody autora

❗ Wszelkie próby przywłaszczenia autorstwa mogą zostać uznane za plagiat



📌 Wersja: 1.4.0

🧾 Autor pluginu: eXotic (michal1337k)


## 🎥 Prezentacja pluginu

https://www.youtube.com/watch?v=nGlVJGkyghs


## ⚙️ Funkcje

✔️ Automatyczne eventy Battle Royale

✔️ System kolejki graczy (/br join)

✔️ Teleportacja na arenę

✔️ Losowe spawn pointy

✔️ Skrzynki z bronią (loot)

✔️ Zapisywanie i przywracanie ekwipunku gracza

✔️ Nagrody dla zwycięzcy

✔️ Komendy admina (start/stop/kick/add)

✔️ Obsługa wielu języków (EN + PL)


## 📦 Instalacja

1. Pobierz plik pluginu BattleRoyal.cs
2. Wgraj go do folderu:
   ```bash
   /server/HurtworldDedicated_Data/Managed/Oxide/Plugins/
   ```
3. Uruchom lub zrestartuj serwer
4. Plugin automatycznie wygeneruje plik konfiguracyjny


## 🛠️ Konfiguracja

Plik konfiguracyjny znajduje się w:
   ```bash
   /oxide/config/BattleRoyal.json
   ```

Przykładowe ustawienia:

   ```bash
   {
      "awardID": 144,
      "awardAmount": 1,
      "minPlayersToStart": 2,
      "eventSlots": 3,
      "eventIntervalMinutes": 3,
      "startTimeSeconds": 5,
      "ChestSpawnPoints": [...],
      "AmmoAtStartEvent": [...]
  }
   ```


Opis parametrów:

- awardID – ID nagrody dla zwycięzcy
- awardAmount – ilość nagrody
- minPlayersToStart – minimalna liczba graczy na serwerze
- eventSlots – liczba miejsc w evencie
- eventIntervalMinutes – co ile minut event się uruchamia
- startTimeSeconds – czas od otwarcia kolejki do startu
- ChestSpawnPoints – miejsca spawnów skrzynek
- AmmoAtStartEvent – ammo dla graczy na start


## 🎮 Komendy
## Dla graczy:

| Komenda    | Działanie |
| -------- | ------- |
| /br join | dołączenie do eventu |
| /br info | informacje o pluginie |
| /br | Podczas trwania eventu: pokazuje liczbę graczy |


## Dla administratora:

| Komenda    | Działanie |
| -------- | ------- |
| /br zapisy | włącza/wyłącza zapisy |
| /br start | ręczny start eventu |
| /br stop | zatrzymanie eventu |
| /br kick <nick> | wyrzucenie gracza z eventu |
| /br add <nick> | dodanie gracza do eventu |



## 🔐 Uprawnienia

Uprawnienia: 
   ```bash
   battleroyal.admin
   ```

Aby dodać:
   ```bash
oxide.grant group admin battleroyal.admin  
```

## 🧠 Jak to działa?

1. Plugin cyklicznie sprawdza liczbę graczy na serwerze
2. Jeśli warunki są spełnione:
 - otwiera się kolejka
 - gracze zapisują się przez /br join
3. Po czasie:
 - gracze są teleportowani na dedykowaną arenę stworzoną specjalnie pod event
 - ich ekwipunek jest zapisywany i czyszczony
4. Na arenie pojawiają się skrzynki z bronią
5. Ostatni żywy gracz wygrywa 🎉
6. Ekwipunek zostaje przywrócony

