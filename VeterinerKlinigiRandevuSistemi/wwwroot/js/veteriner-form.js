//Veteriner formunda sadece seçili kliniğin hizmetlerini gösterir Klinik değişince diğer kliniklerin hizmet kutucukları gizlenir ve işaretleri kaldırılır.
$(function () {
    function hizmetleriFiltrele() {
        var klinikId = $('#KlinikId').val();

        $('.hizmet-secenegi').each(function () {
            var uygun = String($(this).data('klinik-id')) === klinikId;
            $(this).toggle(uygun);
            if (!uygun) {
                $(this).find('input[type=checkbox]').prop('checked', false);
            }
        });
        $('#hizmetYok').toggle($('.hizmet-secenegi:visible').length === 0);
    }

    $('#KlinikId').on('change', hizmetleriFiltrele);
    hizmetleriFiltrele(); //sayfa ilk açıldığında da çalışır.
});