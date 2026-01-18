class Player:
    def __init__(self, name):
        self.name = name

        sql = '''SELECT id, location, round_points, player_time, player_speed, player_range
                FROM game
                WHERE screen_name = %s;'''

        cursor = connect.cursor()
        cursor.execute(sql, (self.name,))
        results = cursor.fetchall()

        # Jos vanha pelaaja löytyy, käytetään tätä
        if len(results) == 1:
            print("Pelaaja löydetty")
            self.location = results[0][1]
            self.points = results[0][2]
            self.time = results[0][3]
            self.speed = results[0][4]
            self.range = results[0][5]
            self.player = True

        # Jos on uusi pelaaja, käytetään tätä
        else:
            print("uusi pelaaja")
            parameters1 = (0, 10080, 60, 100, self.name, "EFHK")
            sql1 = '''INSERT INTO game (round_points, player_time, player_speed, 
                player_range, screen_name, location)
                VALUES (%s, %s, %s, %s, %s, %s);'''

            cursor = connect.cursor()
            cursor.execute(sql1, parameters1)
            connect.commit()

            # valitaan uuden pelaajan luodut tiedot
            sql2 = '''SELECT id, location, round_points, player_time, player_speed, player_range
                FROM game
                WHERE screen_name = %s;'''

            cursor = connect.cursor()
            cursor.execute(sql2, (self.name,))
            results = cursor.fetchall()
            if len(results) == 1:  # määritellään arvot
                self.location = results[0][1]
                self.points = results[0][2]
                self.time = results[0][3]
                self.speed = results[0][4]
                self.range = results[0][5]
                self.player = False

                # Etsitään vielä erikseen molemmissa tapauksissa pelaajan koordinaatit matkojen laskua varten vertaamalla sijaintia airport identtiin
        sql3 = '''SELECT latitude_deg, longitude_deg
                FROM airport
                WHERE airport.ident = %s;'''
        cursor = connect.cursor()
        cursor.execute(sql3, (self.location,))
        coords = cursor.fetchall()
        self.latitude = coords[0][0]
        self.longitude = coords[0][1]



    # Pelaajan liikkuminen sijainnista toiseen sekä päivitys tietokantaan
    def travel(self, airport):
        distance = calculate_distance(self.latitude, self.longitude, airport.latitude,
                                      airport.longitude)  # kulutettu matka
        self.range = self.range - distance  # rangea jäljellä
        self.location = airport.ident  # pelaajan uusi sijainti



    # tallentaa edistyksen sql
    def save_to_sql(self):
        params = (self.points, self.time, self.speed, self.range, self.location, self.name)
        sql = '''UPDATE game
        SET round_points = %s, player_time = %s, player_speed = %s, player_range = %s, location = %s
        WHERE screen_name = %s;'''

        cursor = connect.cursor()
        cursor.execute(sql, params)
        connect.commit()



# lentokenttä olio
class Airport:
    def __init__(self, ident, visited =False, data=None):
        self.ident = ident
        self.visited = visited
        if data is None:
            parameters = (self.ident,)
            sql = '''SELECT ident, name, latitude_deg, longitude_deg, type
            FROM airport
            WHERE airport.ident = %s;'''
            cursor = connect.cursor()
            cursor.execute(sql, parameters)
            result = cursor.fetchall()
            if len(result) == 1:

                self.name = result[0][1]
                self.latitude = float(result[0][2])
                self.longitude = float(result[0][3])
                self.type = result[0][4]
            else:
                self.name = data['name']
                self.latitude = float(data['latitude'])
                self.longitude = float(data['longitude'])



# päivittää vieraillun kentän tietokantaan
    def update_visited(self, player):

        cursor = connect.cursor()
        sql = '''SELECT COUNT(*) FROM visited
        WHERE visited_airport_id = %s AND player_visited_id = %s'''
        cursor.execute(sql, (self.ident, player.name))
        result = cursor.fetchone()
        count = result[0]

        if count == 0:
            sql = '''INSERT INTO visited (visited_airport_id, player_visited_id) VALUES (%s, %s)'''
            cursor.execute(sql, (self.ident, player.name))
            connect.commit()
            self.visited =True
        else:
            pass


#tarkastaa onko kentällä vierailtu
    def check_visited(self, player):

        cursor = connect.cursor()
        sql = '''SELECT COUNT(*) FROM visited
        WHERE visited_airport_id = %s AND player_visited_id = %s'''
        cursor.execute(sql, (self.ident, player.name))
        result = cursor.fetchone()
        count = result[0]

        if count == 0:
            self.visited = False
        else:
            self.visited = True

        return self.visited
