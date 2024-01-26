from flask import Flask, jsonify, request
from flask_cors import CORS
from autogenService import setup_autogen, process_chat_input

app = Flask(__name__)
CORS(app)

user_proxy, manager = setup_autogen()


@app.route('/chat', methods=['POST'])
def chat_interaction():
    data = request.json
    user_input = data.get('input', '')
    responses = process_chat_input(user_proxy, manager, user_input)
    return jsonify(responses)


if __name__ == '__main__':
    app.run(debug=True, port=5001)
