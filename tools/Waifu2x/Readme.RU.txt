﻿waifu2x-конвертер (для Windows)
=================================
 Создано: Amigo (WhiteLuckers)

Это программное обеспечение, единственная функция преобразования программного преобразования изображений "waifu2x (https://github.com/nagadomi/waifu2x)",
Переписать с использованием языка C ++ и OpenCV waifu2x.py (https://marcan.st/transf/waifu2x.py) в качестве ссылки,
Является ли программа, которая была встроена в Windows.
GitHub хранилище будет [здесь] (https://github.com/WL-Amigo/waifu2x-converter-cpp).


 Операционная среда
----------

Это подтвердил операцию в следующих средах.

 * OS: Windows7 64bit (по крайней мере, вы должны быть ** 64-битная WindowsOS **)
 * Процессор: (то, что является хорошим большое количество часов процессоров в максимально возможной степени) Intel на базе процессора
 * Память: 6GB или более (но переехал даже 4 Гб ПК, это программное обеспечение будет съедать много памяти во время работы)
 * То установлен C ++ 2013 распространяемый пакет Визуальный (обязательно)
    - Пакет [здесь] (https://www.microsoft.com/ja-jp/download/details.aspx?id=40784)
    - `После того, как вы нажмете кнопку download`, выберите` vcredist_x64.exe`, убедитесь, чтобы загрузить и установить.
    - Если вы не можете найти, попытаться найти "Visual C ++ 2013 распространяемого пакета".

Он также может работать в других, кроме указанных выше условий, но это работает я не гарантирую.
Кроме того, это не гарантирует того, что вы работать в более чем окружающей среды.
В том числе и тот факт, что не знаю, спасибо для использования в собственной ответственности.


 Как использовать
--------

Это программное обеспечение представляет собой инструмент командной строки.
`Запустите команду prompt`, управляя командой, как показано в следующем, пожалуйста, используйте.


Следующий вывод команды, как использовать экран.
`` `
waifu2x-converter.exe --help
`` `

Следующая команда является примером команды для выполнения преобразования изображения.
`` `
waifu2x-converter.exe -i C: \ Users \ амиго \ Pictures \ mywaifu.png -m noise_scale -j 8 --scale_ratio 1.6 --noise_level 2
`` `
При запуске выше, `C: \ Users \ Amigo \ Pictures \ mywaifu (noise_scale) (Уровень 2) (x1.600000) .png` результат преобразования будет сохранен.


В этом программном обеспечении, можно указать следующие параметры.

   -i <строка>, --input_file <строка>
     (Обязательно) Путь к изображению, чтобы быть преобразованы. (Рекомендуется ввести полный путь)

   -o <строка>, --output_file <строка>
     Путь к файлу, который вы хотите сохранить преобразованный файл (рекомендуется ввести полный путь)
     Extension (например, последний .png) Убедитесь в том, чтобы войти.
     Если вы не указываете, чтобы автоматически определить имя файла и сохранить его в файле.
     Правила принятия решений в имени файла,
     `(В случае уровня удаления шумов (режим удаления шума)) [исходное имя файла изображения]` `(имя режима)` `` `(коэффициент увеличения (в случае режима расширения))` `.png`
     Похоже.
     Расположение Под сохранением в основном будет находиться в том же каталоге, что и исходное изображение.
     (Если вы написали путь к входному изображению в относительном пути, есть вероятность того, что что неожиданное происходит)

   -m <шум | шкала | noise_scale>, --mode <шум | шкала | noise_scale>
     Укажите режим преобразования. Если вы не указать `noise_scale` он будет выбран.
      * Уровень шума: сделать удаление шума (точнее, сделать преобразование изображения с использованием модели для удаления шума)
      * Масштаб: сделать расширение (точнее, после того, как увеличивается с помощью существующего алгоритма, сделать преобразование изображения, используя модель для увеличенного изображения дополнения)
      * Noise_scale: сделать расширение и шума удаление (после выполнения удаления шума, будет продолжать выполнять процесс расширения)

   -j <целое>, --jobs <целое значение>
     Он определяет число деления обработки в программе. Нить числа, указанного в этой опции будет запущен в рамках программы.
     Но вы можете почти обработку быстрее, указав количество ядер процессора, так что вы хотите использовать процессор на 100 процентов,
     Пожалуйста, обратите внимание на тепловыделении и тому подобное.
     Значение по умолчанию равно `4`.

   --scale_ratio <точка с номером>
     Он указывает, будет ли расширяться во много раз. Значение по умолчанию `2.0`, но вы также можете указать другой, чем в 2,0 раза.
     Если вы укажете значение, отличное от 2,0, выполните следующую обработку.
      * В первую очередь, для покрытия необходимо и достаточно, чтобы указанное увеличение, повторите дважды расширение.
      * Если количество неэнергетических-оф-2, вы можете уменьшить увеличенное изображение будет указано увеличение в линейном фильтре.

   --noise_level <1 | 2>
     Укажите уровень удаления шума. Так как модель для удаления шума получают только 1-го уровня и 2-го уровня,
      Пожалуйста, укажите один или два.
     Значение по умолчанию равно `1`.

   --model_dir <строка>
     Модель определяет путь к каталогу, который хранится. Значение по умолчанию равно `models`.
     Является ли в принципе все в порядке, даже если вы не укажете. Пожалуйста, укажите, например, когда вы хотите использовать свою собственную модель.

   -, --ignore_rest
     Игнорировать все варианты после того, как этот вариант был.
     Это для сценария пакетного файла.

   --version
     Информация о версии Выход и выход.

   -h, --help
     Он показывает, как использовать, и выход.
     Пожалуйста, например, когда вы хотите легко увидеть, как использовать его.


 История изменений
----------

 * V1.0.0: Первое издание
 * V1.1.0:
    - Исправлена ​​ошибка, которая, как представляется, вокруг смазыванию изображения по сравнению с оригинальной версией
    - Добавление сегментации изображения внутренней обработки заказа, чтобы сохранить низкое потребление памяти (выход, как обычно)
 * V1.1.1:
    - Исправлена ​​проблема, попадающие в случае возникновения ошибки на выходе конкретного размера изображения (около ширины или высоты составляет 512)


 отказываться
------------

Запросы об этой программе является спасибо вам, чтобы не быть как можно больше.
Неизвестно вещь, поиск как можно больше, не забудьте себе решимости.

Даже если запрос, и что обязанность ответить на вопрос, а не предпринимать какой-либо предмет, в том числе автор этого программного обеспечения.


 квитирование
------
Оригинальный waifu2x и выполняет производство модели, Ultraist подобное, которые опубликованы под лицензией MIT,
Оригинальный Гектор Мартин, как, что, кто создал преобразование для сценария питона на основе waifu2x (Тара это не так, я не думал, пытаясь убедиться),
Одуванчика, как кто давал советы в процессе до общественности, Вы писали чувство благодарности.