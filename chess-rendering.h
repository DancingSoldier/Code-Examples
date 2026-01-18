#include <iostream>
#include "../headers/shakki.h"
#include "../headers/asema.h"
#include "../headers/player_input.h"
#include <raylib.h>
#include <vector>
#include <map>
#include <string>

using namespace std;


/*!
SCREEN_WIDTH = ikkunan leveys.
SCREEN_HEIGHT = ikkunan korkeus.
FRAMES_PER_SECOND = kuinka monta kertaa sekunnissa ikkuna p�ivitt�� itsens�.
*/
constexpr short SCREEN_WIDTH = 1280;
constexpr short SCREEN_HEIGHT = 800;
constexpr short FRAMES_PER_SECOND = 60;

/*!
textureMap tekee hash tablen tekstuurien nimest� ja tiedostosta.

tekstuurin_nimi: tekstuuri
*/
map<std::string, Texture2D> textureMap;

/*!
P��luokka pelin k�ytt�liittym�lle.
*/
class Renderer
{
private:
	const short _posX = 60; /*!< Laudan piirt�misen aloituspiste, leveys.*/
	const short _posY = 60; /*!< Laudan piirt�misen aloituspiste, korkeus.*/
	
	const short _rect_width = 60; /*!< Yhden ruudun leveys.*/
	const short _rect_height = 60; /*!< Yhden ruudun korkeus.*/
	
	const short frame_size = 30; /*!< Laudan reunuksen leveys.*/
	
	const Color color1 = WHITE; /*!< Valkoinen v�ri.*/
	const Color color2 = RED; /*!< Punainen v�ri.*/
	const Color frame_color = YELLOW; /*!< Laudan reunuksen v�ri, keltainen.*/

	Rectangle _text_box; /*!< Laatikko pelaajan sy�tett� varten.*/

public:
	/*!
	Peli-ikkunan alustus.
	1. InitWindow() luo ikkunan SCREEN_WIDTH, SCREEN_HEIGHT -ehdoilla ja nime�� sen
	   "Shakkibitch".
	2. SetTargetFPS() asettaa FRAMES_PER_SECOND:in p�ivitystaajuudeksi.
	3. _text_box sijoitetaan n�yt�lle.
	4. LoadTextures() lataa nappuloiden omat kuvat.
	*/
	void InitializeWindow()
	{
		InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Shakkibitch");
		SetTargetFPS(FRAMES_PER_SECOND);

		_text_box = { SCREEN_WIDTH / 2 - 610, SCREEN_HEIGHT / 2 + 200, 200, 50 };

		LoadTextures();
	 }

	/*!
	Joka ruudunp�ivityksell� tapahtuva tilanteen p�ivitys.

	Piece board[8][8] = lauta, joka piirret��n n�yt�lle.
	
	ClearBackground() 
	DrawBoard() 
	UpdatePieces() 
	DrawRectangleRec() ja DrawRectangleLinesEx() piirt�v�t sy�telaatikon n�yt�lle.
	DrawText() piirt�� pelaajalle tietoa n�yt�lle.
	*/
	void UpdateWindow(Piece board[8][8], color turn, bool current) const
	{
		ClearBackground(BLACK); /*!< V�rj�� koko ruudun mustaksi.*/
		DrawBoard(); /*!< Piirt�� tyhj�n laudan n�yt�lle.*/
		UpdatePieces(board); /*!< P�ivitt�� nappulat laudalle annetun laudan mukaan.*/

		DrawRectangleRec(_text_box, LIGHTGRAY); /*!< Piirt�� sy�telaatikon taustan.*/
		DrawRectangleLinesEx(_text_box, 2, BLUE); /*!< Piirt�� sy�telaatikon reunat.*/

		DrawText("Click on the box and write your move in a1a2 form.",
			20,
			SCREEN_HEIGHT - 140,
			20,
			RAYWHITE); /*!< Piirt�� pelaajalle tietoa sy�tteest�.*/


		if (turn == PALE)
		{
			DrawText("WHITE'S TURN",
				20,
				SCREEN_HEIGHT - 70,
				40,
				RAYWHITE); /*!< Kun on valkoisen vuoro.*/
		}
		else
		{
			DrawText("BLACK'S TURN",
				20,
				SCREEN_HEIGHT - 70,
				40,
				RAYWHITE); /*!< Kun on mustan vuoro.*/
		}

		DrawText("Press SPACE to let the artificial intelligence\nmake the next move.",
			SCREEN_WIDTH / 2 - 20,
			50,
			20,
			RAYWHITE); /*!< Piirt�� pelaajalle ohjeen teko�lyn k�yt�st�.*/

		DrawText("Press LEFT or RIGHT ARROW to rollback and observe\nprevious situations.",
			SCREEN_WIDTH / 2 - 20,
			100,
			20,
			RAYWHITE); /*!< Piirt�� pelaajalle ohjeen aikaisempien tilanteiden k�ytt��n.*/

		if (current)
			DrawText("Current situation!",
				SCREEN_WIDTH / 2 - 20,
				150,
				30,
				RAYWHITE); /*!< Piirt�� pelaajalle, onko n�yt�ll� oleva tilanne viimeisin.*/
	}

	/*!
	Palauttaa Texture2D:n annetun nimen perusteella textureMap:ista.

	const string& texture = ladattavan tekstuurin nimi.
	*/
	Texture2D GetPieceTexture(const string& texture) const
	{
		return textureMap[texture];
	}

	/*!
	Rakentaa tekstuurin nimen tekstimuotoon annetun nappulan perusteella.

	Piece& unit = nappula, jonka perusteella teksti rakennetaan.
	*/
	string BuildString(Piece& unit) const
	{

		string piece_color = "";
		string piece_type = "";
		if (unit.color == 0)
		{
			piece_color = "b_";
		}
		else if (unit.color == 1) {
			piece_color = "w_";
		}
		else
		{
			piece_color = "";
		}

		switch (unit.type)
		{
		case ROOK:
			piece_type = "rook";
			break;
		case KNIGHT:
			piece_type = "knight";
			break;
		case BISHOP:
			piece_type = "bishop";
			break;
		case QUEEN:
			piece_type = "queen";
			break;
		case KING:
			piece_type = "king";
			break;
		case PAWN:
			piece_type = "pawn";
			break;
		default:
			piece_type = "";
			break;
		}

		string texture_name = piece_color + piece_type;
		return texture_name;
	}

	/*!
	Lataa textureMap:iin tekstuurit nimen perusteella.
	*/
	void LoadTextures() const
	{
		std::vector<std::string> pieceNames = {
			"b_rook", "b_knight", "b_bishop", "b_queen", "b_king", "b_pawn",
			"w_rook", "w_knight", "w_bishop", "w_queen", "w_king", "w_pawn"
		};

		for (const std::string& name : pieceNames) {
			std::string path = "../graphics/" + name + ".png";
			textureMap[name] = LoadTexture(path.c_str());
		}
	}

	/*!
	Piirt�� tyhj�n laudan.
	*/
	void DrawBoard() const
	{
		DrawRectangle(_posX - frame_size,
					  _posY - frame_size,
					  _rect_width * 8 + 2 * frame_size,
					  _rect_height * 8 + 2 * frame_size,
					  frame_color); /*!< Reunojen piirto, alkuper�inen sijainti - reunan koko.*/


		//! P�yd�n piirto.
		for (short row = 0; row < 8; row++)
		{
			for (short col = 0; col < 8; col++)
			{
				//! Ruudun piirto.
				int position_x = _posX + col * _rect_width;
				int position_y = _posY + row * _rect_height;
				Color square_color = (row + col) % 2 == 0 ? color1 : color2;
				DrawRectangle(position_x, position_y, _rect_width, _rect_height, square_color);
			}
		}

		//! Numerot ruuduille.
		for (short row = 0; row < 8; row++)
		{
			int number = 8 - row;
			int text_pos_y = _posY + row * _rect_height + _rect_height / 2 - 10; /*!< Keskitet��n.*/
			DrawText(TextFormat("%d", number), _posX - frame_size + 5, text_pos_y, 20, BLACK); /*!< Vasemmalle.*/
			DrawText(TextFormat("%d", number), _posX + _rect_width * 8 + frame_size - 15, text_pos_y, 20, BLACK); /*!< Oikealle.*/
		}
		//! Kirjaimet ruuduille.
		for (short col = 0; col < 8; col++)
		{
			char letter = 'A' + col;
			int text_pos_x = _posX + col * _rect_width + _rect_width / 2 - 5; /*!< Keskitet��n.*/
			DrawText(TextFormat("%c", letter), text_pos_x, _posY - frame_size + 5, 20, BLACK); /*!< Yl�s.*/
			DrawText(TextFormat("%c", letter), text_pos_x, _posY + _rect_height * 8 + frame_size - 20, 20, BLACK); /*!< Alas.*/
		}
	}

	/*!
	P�ivitet��n nappuloiden sijainnit laudalla.

	Piece board[8][8] = lauta, jolta nappuloiden sijainnit saadaan.
	*/
	void UpdatePieces(Piece board[8][8]) const
	{

		for (short row = 0; row < 8; row++)
		{
			for (short column = 0; column < 8; column++)
			{
				Piece piece_to_create = board[row][column];
				if (piece_to_create.type == EMPTY || piece_to_create.type == PASSANT)
					continue;

				string image_str = BuildString(piece_to_create);
				Texture2D texture = GetPieceTexture(image_str);

				DrawTextureEx(texture,
							  Vector2{ (float)(column * 60) + (_rect_width / 2) + (_posX / 2),
							  (float)(row * 60) + (_rect_height / 2) + (_posY / 2) },
							  0,
							  1,
							  RAYWHITE);
			}
		}
	}

	/*!
	Pelaajan sy�tteen p�ivitt�minen tekstilaatikkoon.

	char text[MAX_INPUT_CHARS + 1] = laatikossa esitett�v� teksti.
	*/
	void UpdateTextBox(char text[MAX_INPUT_CHARS + 1]) const
	{
		DrawText(text, (int)_text_box.x + 5, (int)_text_box.y + 15, 20, BLACK);
	}

	/*!
	Vapauttaa ladatut tekstuurit.
	*/
	void UnloadTextures()
	{
		for (const auto& texture : textureMap) {
			UnloadTexture(texture.second);
		}
		textureMap.clear();
	}
};
