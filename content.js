// content.js

// Function to capture selected text and send it to the Humanizer API
function sendSelectedTextToHumanizer() {
    // Get selected text from the document
    const selectedText = window.getSelection().toString();

    if (selectedText) {
        // Prepare the data for the API request
        const data = { text: selectedText };

        // Send the selected text to the Humanizer API
        fetch('https://api.humanizer.com/transform', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        })
        .then(response => response.json())
        .then(result => {
            console.log('Humanized text:', result.humanizedText);
            // You can add additional logic to handle the result here
        })
        .catch(error => console.error('Error:', error));
    } else {
        console.log('No text selected.');
    }
}

// You can call this function from an event listener or a button click
// For example:
// document.getElementById('your-button-id').addEventListener('click', sendSelectedTextToHumanizer);