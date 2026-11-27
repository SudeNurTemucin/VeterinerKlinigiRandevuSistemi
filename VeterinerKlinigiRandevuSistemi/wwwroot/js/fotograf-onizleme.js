//Fotoğraf seçilince yüklemeden önce önizleme gösterir.jQuery + FileReader kullanırız.
$(function () {
    $('#Fotograf').on('change', function () {
        var dosya = this.files && this.files[0];
        var $onizleme = $('#onizleme');

        if (!dosya || !dosya.type.startsWith('image/')) {
            $onizleme.addClass('d-none').attr('src', '');
            return;
        }

        var okuyucu = new FileReader();
        okuyucu.onload = function (e) {
            $onizleme.attr('src', e.target.result).removeClass('d-none');
        };
        okuyucu.readAsDataURL(dosya);
    });
});