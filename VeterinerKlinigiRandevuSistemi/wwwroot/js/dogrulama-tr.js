// Bu dosya, "sayı mı?" ve "aralıkta mı?" kurallarını virgülü kabul edecek şekilde değiştirir.
(function ($) {
    if (!$ || !$.validator) {
        return;
    }

    function sayiyaCevir(deger) {
        return parseFloat(String(deger).replace(',', '.'));
    }

    //450 veya 450,50 geçerli; 450.50 geçersiz.
    $.validator.methods.number = function (deger, eleman) {
        return this.optional(eleman) || /^-?\d+(,\d+)?$/.test(deger);
    };

    //Kurallar virgüllü sayılarla da çalışsın.
    $.validator.methods.range = function (deger, eleman, parametre) {
        var sayi = sayiyaCevir(deger);
        return this.optional(eleman) || (sayi >= parametre[0] && sayi <= parametre[1]);
    };
})(window.jQuery);
