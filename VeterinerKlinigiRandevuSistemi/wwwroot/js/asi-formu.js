//+1 ay / +3 ay / +1 yıl butonları, yapıldığı tarihe ekleyip sonraki doz tarihini doldurur.
$(function () {
    $('.doz-ekle').on('click', function () {
        var yapilma = $('#YapilmaTarihi').val();  
        if (!yapilma) {
            return;
        }

        var parcalar = yapilma.split('-');
        var tarih = new Date(parcalar[0], parcalar[1] - 1, parcalar[2]);
        tarih.setMonth(tarih.getMonth() + parseInt($(this).data('ay'), 10));

        var yil = tarih.getFullYear();
        var ay = String(tarih.getMonth() + 1).padStart(2, '0');
        var gun = String(tarih.getDate()).padStart(2, '0');
        $('#SonrakiDozTarihi').val(yil + '-' + ay + '-' + gun);
    });
});