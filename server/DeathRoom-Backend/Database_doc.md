# Documentation for Database namespace

Allows connection and interaction with postgresql database

## Structs

### User

Struct for working with user data

*Fields*

- user_id           : int       - unique user id

- login             : str       - unique login

- hashed_password   : str       - hashed (to prevent sql injection and data stealing) user password

- nickname          : str       - not unique name that other players see

- rating            : int       - integer representation of player skills

- last_seen         : datetime  - date and time of player last logout

### PlayerResults

Structs for storing personal results of player in exact match

*Fields*

- user_id           : int       - contains unique user id

- kills             : int       - number of kills performed by user in match

- deaths            : int       - number of times user died in match

### Match

Struct for storing general match results

*Fields*

- match_id          : str               - unique id of match

- matchResults      : PlayerResults[]   - array of personal results of each player in match

## Database Class

### Fields

- source    (private)   : NpgsqlDataSource  - object for database interaction

### Constructors

- public Database(string)           - Creates database connection from string of type "Host=[hostname];Username=[username];Password=[password];Database=[database name]"

### Methods

- RegisterUser(User)                : void              - Creates user instance in the 'Player' table

- RetrieveUser(string)              : User              - Searches the 'Player' table for exact user id

- RetrieveUser(int)                 : User              - Searches the 'Player' table for exact user login

- SaveMatchResults(MatchResults)    : void              - Creates instance in the 'Match' table for match and instances in 'MatchPlayed' table for each match participant

- MatchHistory(int)                 : PlayerResults[]   - Searches personal results for each match player participated with his id
- MatchResults(str)                 : Match             - Searches for results of match by match id
