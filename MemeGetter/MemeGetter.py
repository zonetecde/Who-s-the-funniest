# page 1 � 54 https://imgflip.com/memetemplates?page=1
import urllib.request
import re
import os
from os import path
import requests
import html

def download_meme_list():
            
    meme_template = []
    gif_template = []
        
    for i in range(1, 54):

        lien = "https://imgflip.com/memetemplates?sort=top-all-time&page=" + str(i)
        r = requests.get(lien)
        # récupère l'html en le décodant 
        content = html.unescape(r.text)
        
        # récupère les mêmes
        memes_html = re.findall(r'<img class="shadow" style="width(.+?)/>', content)
    
        # si  Meme Template : meme
        # si  GIF Template : gif
       
        for meme_html in memes_html:
            # même image
            if "Meme Template" in meme_html:
                # nom_du_meme,lien
                nom = re.search('" alt="(.*) Meme Template"', meme_html).group(1)
                id = re.search('//i.imgflip.com/4/(.*)"', meme_html).group(1)
                meme_template.append(str.replace(nom, "|", "/") + "|" + "https://i.imgflip.com/" + id)
            elif "GIF Template" in meme_html:
                # nom_du_meme,lien
                nom = re.search('" alt="(.*) GIF Template"', meme_html).group(1)
                id = re.search('//i.imgflip.com/2/(.*).jpg', meme_html).group(1) + ".mp4"
                
                gif_template.append(str.replace(nom, "|", "/") + "|" + "https://i.imgflip.com/" + id)
        
        print("page ", str(i), "/53")
        
    # ajoute les mêmes gif
    for i in range(1, 84):

        lien = "https://imgflip.com/gif-templates?page=" + str(i)
        r = requests.get(lien)
        # récupère l'html en le décodant 
        content = html.unescape(r.text)
        
        # récupère les mêmes gif
        memes_html = re.findall(r'<img class="shadow" style="width(.+?)/>', content)
    
        for meme_html in memes_html:
            if "GIF Template" in meme_html:
                # nom_du_meme,lien
                nom = re.search('" alt="(.*) GIF Template"', meme_html).group(1)
                id = re.search('//i.imgflip.com/2/(.*).jpg', meme_html).group(1) + ".mp4"
                
                gif_template.append(str.replace(nom, "|", "/") + "|" + "https://i.imgflip.com/" + id)
        
        print("page ", str(i), "/ 83")
        
    # remove duplicate
    meme_template = list(dict.fromkeys(meme_template))
    gif_template = list(dict.fromkeys(gif_template))
    
    return (meme_template, gif_template)
    
(meme, gif) = download_meme_list()

f = open("memes.txt", "w", encoding="utf-8")

for item in meme:
    f.write(item + "\n")
f.close()

f = open("memes_gif.txt", "w", encoding="utf-8")

for item in gif:
    f.write(item + "\n")

f.close()
