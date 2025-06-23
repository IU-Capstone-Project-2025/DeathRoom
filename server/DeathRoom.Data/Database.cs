using Npgsql;

namespace Database;


// Structs for handling database interacting
struct User {
	public int user_id;
	public string login;
	public string hashedPassword;
	public string nickname;
	public int rating;
	public DateTime last_seen;
}

struct PlayerResults {
	public int user_id;
	public int kills;
	public int deaths;
}

struct Match {
	public str match_id;
	public PlayerResults[] matchResults;
}


// Class for working with database
public class Database {

	private NpgsqlDataSource source;
	
	public Database(string connectionString) {
		await using var this.source = NpgsqlDataSource.Create(connectionString);

		// Table creation (if not exists)
		// Player table
		await using var command = this.source.CreateCommand("create table if not exists Player ( player_id int, primary key (player_id), login varchar(20) unique, hashed_password varchar(50), nickname varchar(20), rating integer, lastseen date);");
		await command.ExecuteNonQueryAsync();
		// Match table
		await using var command = this.source.CreateCommand("create table if not exists Match ( match_id int, primary key (match_id));");
		await command.ExecuteNonQueryAsync();
		// MatchPlayed table
		await using var command = this.source.CreateCommend("create table if not exists MatchPlayed ( player_id int references Player(player_id), match_id int references Match(match_id), kills int, deaths int);");
		await command.ExecuteNonQueryAsync();

		// Indexes creation (if not exists)
		await using var command = this.source.CreateCommand("create index if not exists pl_id on player using hash (player_id);");
		await command.ExecuteNonQueryAsync();

		await using var command = this.source.CreateCommand("create index if not exists pl_li on player using hash (login);");
		await command.ExecuteNonQueryAsync();

		await using var command = this.source.CreateCommand("create index if not exists mt_id on match using hash (match_id);");
		await command.ExecuteNonQueryAsync();

		await using var command = this.source.CreateCommand("create index if not exists mp_pl on matchPlayed using hash (player_id);");
		await command.ExecuteNonQueryAsync();

		await using var command = this.source.CreateCommand("create index if not exists mp_mt on matchplayed using hash (match_id);");
		await command.ExecuteNonQueryAsync();
	}

	// User interactions
	public void RegisterUser(User userData) {
		await using var conn = await this.source.OpenConnectionAsync();
		await using var trans = await conn.BeginTransactionAsync();
		await using var command = new NpgsqlCommand("insert into player values ($1,$2,$3,$4,$5,$6);",conn,trans) {
			Parameters = {
				new() { Value = userData.user_id },
				new() { Value = userData.login },
				new() { Value = userData.hashed_password },
				new() { Value = userData.nickname },
				new() { Value = userData.rating },
				new() { Value = userData.lastseen }
			}
		};
		await command.ExecuteNonQueryAsync();
		await transaction.CommitAsync();
	}
	public User RetrieveUser(int userId) {
		await using var command = this.source.CreateCommand("select * from player where player_id=$1;") { Parameters = { new() { Value = userId } } };
		await using var reader = await command.ExecuteReaderAsync();
		await async reader.ReadAsync();
		User user = new User {
			user_id = reader.GetInt(0);
			login = reader.GetString(1);
			hashed_password = reader.GetString(2);
			nickname = reader.GetString(3);
			rating = reader.GetInt(4);
			last_seen = reader.GetDateTime(5);
		};
		return user;
	}
	public User RetrieveUser(string login) {
		await using var command = this.source.CreateCommand("select * from player where login=$1;") { Parameters = { new() { Value = login } } };
		await using var reader = await command.ExecuteReaderAsync();
		string userRaw = reader.GetString(0);
		User user = new User {
			user_id = reader.GetInt(0);
			login = reader.GetString(1);
			hashed_password = reader.GetString(2);
			nickname = reader.GetString(3);
			rating = reader.GetInt(4);
			last_seen = reader.GetDateTime(5);
		};
		return user;
	}

	// Match results and history
	public void SaveMatchResults(MatchResults res) {
		await using var conn = await this.source.OpenConnectionAsync();
		await using var trans = await conn.BeginTransactionAsync();
		await using var command = new NpgsqlCommand("insert into match values ($1);",conn,trans) {
			Parameters = {
				new() { Value = res.match_id }
			}
		};
		await command.ExecuteNonQueryAsync();
		foreach(var persRes in res.matchResults) {
			await using var command = new NpgsqlCommand("insert into matchPlayed values ($1,$2,$3,$4);",conn,trans) {
				Parameters = {
					new() { Value = persRes.player_id },
					new() { Value = persRes.match_id },
					new() { Value = persRes.kills },
					new() { Value = persRes.deaths }
				}
			};
			await command.ExecuteNonQueryAsync();
		}
		await transaction.CommitAsync();
	}
	public PlayerResults[] MatchHistory(int userId) {
		await using var command = this.source.CreateCommand("select count(*) from matchPlayed where player_id=$1;") { Parameters = { new() { Value = userId } } };
		await using var reader = await command.ExecuteReaderAsync();
		await reader.ReadAsync();
		int numMatchesPlayed = reader.getInt(0);
		await using var command = this.source.CreateCommand("select * from matchPlayed where player_id=$1;") { Parameters = { new() { Value = userId } } };
		await using var reader = await command.ExecuteReaderAsync();
		PlayerResults[] history = new PlayerResults[numMatchesPlayed];
		int i = 0;
		while (await reader.ReadAsync()) {
			PlayerResult res = new PlayerResult {
				user_id = reader.GetInt(0),
				kills = reader.GetInt(2),
				deaths = reader.GetInt(3)
			};
			history[i] = res;
			i++;
		}
		return history;
	}
	public Match MatchResults(str matchId) {
		Match res;
		res.match_id = matchId;
		await using var command = this.source.CreateCommand("select count(*) from matchPlayed where match_id=$1;") { Parameters = { new() { Value = matchId } } };
		await using var reader = await command.ExecuteReaderAsync();
		await reader.ReadAsync();
		int numPlayers = reader.getInt(0);
		await using var command = this.source.CreateCommand("select * from matchPlayed where match_id=$1;") { Parameters = { new() { Value = matchId } } };
		await using var reader = await command.ExecuteReaderAsync();
		PlayerResults[] res = new PlayerResults[numPlayers];
		int i = 0;
		while (await reader.ReadAsync()) {
			PlayerResult res = new PlayerResult {
				user_id = reader.GetInt(0),
				kills = reader.GetInt(2),
				deaths = reader.GetInt(3)
			};
			history[i] = res;
			i++;
		}
		res.matchResults = res;
		return res;
	}
}
