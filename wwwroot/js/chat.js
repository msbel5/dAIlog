function handleKeyPress(event) {
    if (event.keyCode === 13) { // 13 is the Enter key
        event.preventDefault();
        sendMessageWithHistory();
    }
}

function sendMessage() {
    var message = document.getElementById('message-input').value;
    var chatBox = document.getElementById('chat-box');

    if (!message.trim()) {
        alert("Please enter a message.");
        return;
    }

    // Add user message to chat
    var userMessageDiv = document.createElement('div');
    userMessageDiv.textContent = `User: ${message}`;
    chatBox.appendChild(userMessageDiv);

    document.getElementById('message-input').value = '';
    document.getElementById('message-input').focus();

    // AJAX call to backend
    fetch('/Home/SendMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ message: message })
    })
        .then(response => response.json())
        .then(data => {
            var gptResponse = data.choices[0].message.content;
            var gptMessageDiv = document.createElement('div');
            gptMessageDiv.textContent = `GPT-3.5: ${gptResponse}`;
            chatBox.appendChild(gptMessageDiv);

            chatBox.scrollTop = chatBox.scrollHeight;
        })
        .catch(error => console.error('Error:', error));
}
function sendMessageWithHistory() {
    var message = document.getElementById('message-input').value;
    var chatBox = document.getElementById('chat-box');

    if (!message.trim()) {
        alert("Please enter a message.");
        return;
    }

    var userMessageDiv = document.createElement('div');
    userMessageDiv.textContent = `User: ${message}`;
    chatBox.appendChild(userMessageDiv);

    document.getElementById('message-input').value = '';
    document.getElementById('message-input').focus();

    // AJAX call to backend for SendMessageWithHistory
    fetch('/Home/SendMessageWithHistory', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ message: message })
    }).then(response => response.json())
        .then(data => {
            if (data.messages) {
                data.messages.forEach(message => {
                    var gptMessageDiv = document.createElement('div');
                    gptMessageDiv.textContent = `GPT-3.5: ${message}`;
                    chatBox.appendChild(gptMessageDiv);
                });
            } else {
                console.error('Error: No messages received');
            }

            chatBox.scrollTop = chatBox.scrollHeight;
        })
        .catch(error => console.error('Error:', error));
}

function sendMessageToAutoGen() {
    var message = document.getElementById('message-input').value;
    var chatBox = document.getElementById('chat-box');

    if (!message.trim()) {
        alert("Please enter a message.");
        return;
    }

    // Add user message to chat
    var userMessageDiv = document.createElement('div');
    userMessageDiv.textContent = `User: ${message}`;
    chatBox.appendChild(userMessageDiv);

    // Clear the input field and refocus
    document.getElementById('message-input').value = '';
    document.getElementById('message-input').focus();

    // AJAX call to backend
    fetch('/Home/SendMessageToAutogen', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ Message: message })
    })
        .then(response => response.json())
        .then(data => {
            if (data && data.responses) {
                data.responses.forEach(response => {
                    var agentMessageDiv = document.createElement('div');
                    agentMessageDiv.textContent = `${response.agent_name}: ${response.message}`;
                    chatBox.appendChild(agentMessageDiv);
                });
            } else {
                console.error('Error: No response data received');
            }

            // Scroll to the bottom of the chat box
            chatBox.scrollTop = chatBox.scrollHeight;
        })
        .catch(error => {
            console.error('Error:', error);
            alert('There was an error processing your request.');
        });
}

