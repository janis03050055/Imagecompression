## 圖片壓縮

You can use the [editor on GitHub](https://github.com/janis03050055/Imagecompression-Kmeans/edit/master/README.md) to maintain and preview the content for your website in Markdown files.

Whenever you commit to this repository, GitHub Pages will run [Janis](https://github.com/janis03050055/) to rebuild the pages in your site, from the content in your Markdown files.

### 程式簡介

**找到此圖的代表性顏色**

1. 先取大於選擇數量的兩倍亂數(避免亂數點分配不均)。
2. 加總各點到群組點的距離，使用差距越大的像素。
3. 運用快速排序法找中間值。
4. 利用中間值找出起始群，另存分群點作為字典進行查詢。


**將相似的顏色歸類在同一群**

1. 選擇與代表性顏色相近(距離最短)的歸類在同一群。
2. 更改為代表性顏色。
3. 進行RLE無失真壓縮(111111000->1 6 0 3)。

PTT說明文件下載：[點我](https://drive.google.com/file/d/0B9Y0oTTJfS1tX0lRVUdDOXo5ek0/view?usp=sharing)
