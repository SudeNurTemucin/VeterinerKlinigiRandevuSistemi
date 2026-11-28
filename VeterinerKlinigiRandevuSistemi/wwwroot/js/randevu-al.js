// Her seçim değişince bir sonraki liste sunucudan json olarak istenir.
$(function () {
    var $form = $('#randevuFormu');
    if ($form.length === 0) {
        return;
    }

    var $klinik = $('#KlinikId');
    var $hizmet = $('#HizmetId');
    var $veteriner = $('#VeterinerId');
    var $tarih = $('#Tarih');
    var $saat = $('#Saat');
    var $saatBilgi = $('#saatBilgi');

    function listeyiDoldur($select, bosMetin, ogeler) {
        $select.empty().append($('<option>').val('').text(bosMetin));
        $.each(ogeler, function (_, oge) {
            $select.append($('<option>').val(oge.deger).text(oge.metin));
        });
        $select.prop('disabled', ogeler.length === 0);
    }

    function saatleriTemizle(mesaj) {
        listeyiDoldur($saat, '-- Saat --', []);
        $saatBilgi.text(mesaj);
    }

    function hizmetleriYukle() {
        listeyiDoldur($hizmet, '-- Önce klinik seçin --', []);
        listeyiDoldur($veteriner, '-- Önce hizmet seçin --', []);
        saatleriTemizle('Önce veteriner ve tarih seçin.');
        if (!$klinik.val()) {
            return $.when();
        }
        return $.getJSON($form.data('hizmetler-url'), { klinikId: $klinik.val() }).done(function (liste) {
            listeyiDoldur($hizmet,
                liste.length ? '-- Hizmet seçin --' : 'Bu klinikte randevu alınabilir hizmet yok',
                $.map(liste, function (h) {
                    return { deger: h.id, metin: h.ad + ' · ' + h.sureDakika + ' dk · ' + h.ucretMetni };
                }));
        });
    }

    function veterinerleriYukle() {
        listeyiDoldur($veteriner, '-- Önce hizmet seçin --', []);
        saatleriTemizle('Önce veteriner ve tarih seçin.');
        if (!$hizmet.val()) {
            return $.when();
        }
        return $.getJSON($form.data('veterinerler-url'), { hizmetId: $hizmet.val() }).done(function (liste) {
            listeyiDoldur($veteriner, '-- Veteriner seçin --',
                $.map(liste, function (v) { return { deger: v.id, metin: v.ad }; }));
        });
    }

    function saatleriYukle() {
        saatleriTemizle('Önce veteriner ve tarih seçin.');
        if (!$veteriner.val() || !$tarih.val()) {
            return $.when();
        }
        $saatBilgi.text('Müsait saatler yükleniyor...');
        return $.getJSON($form.data('saatler-url'), {
            veterinerId: $veteriner.val(),
            hizmetId: $hizmet.val(),
            tarih: $tarih.val()          
        }).done(function (saatler) {
            listeyiDoldur($saat, '-- Saat seçin --',
                $.map(saatler, function (s) { return { deger: s, metin: s }; }));
            $saatBilgi.text(saatler.length
                ? saatler.length + ' müsait saat bulundu.'
                : 'Bu tarihte müsait saat yok. Başka bir gün deneyin.');
        });
    }

    // Form hatayla geri döndüyse önceki seçim geri koyulur.
    function seciliyiGeriKoy($select) {
        var deger = $select.data('secili');
        if (deger && deger !== 0) {
            $select.val(String(deger));
        }
    }

    $klinik.on('change', hizmetleriYukle);
    $hizmet.on('change', veterinerleriYukle);
    $veteriner.on('change', saatleriYukle);
    $tarih.on('change', saatleriYukle);

    // Sayfa açılınca zinciri sırayla çalıştır.
    hizmetleriYukle()
        .then(function () { seciliyiGeriKoy($hizmet); return veterinerleriYukle(); })
        .then(function () { seciliyiGeriKoy($veteriner); return saatleriYukle(); })
        .then(function () { seciliyiGeriKoy($saat); });
});