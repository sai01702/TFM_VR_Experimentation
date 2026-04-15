from gtts import gTTS
print("OK")
import sys
import os

print("PYTHON SCRIPT STARTED")

text = sys.argv[1]

# Save inside Unity project
output_path = os.path.join(os.getcwd(), "Assets/Audio/instruction.mp3")

print("Saving to:", output_path)

tts = gTTS(text=text, lang='en')
tts.save(output_path)

print("DONE")