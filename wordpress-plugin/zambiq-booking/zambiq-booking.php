<?php
/**
 * Plugin Name: Zambiq Booking
 * Description: Sluit de Zambiq-reserveringswidget in via een shortcode. WordPress is enkel een client van de centrale Zambiq-API.
 * Version: 1.0.0
 * Author: Zambiq
 * License: GPL-2.0-or-later
 */

if (!defined('ABSPATH')) {
    exit; // No direct access.
}

const ZAMBIQ_BOOKING_OPTION_HOST = 'zambiq_widget_host';

/**
 * Register the single setting: the public Zambiq widget host (Blazor app origin).
 */
function zambiq_booking_register_settings()
{
    register_setting(
        'zambiq_booking',
        ZAMBIQ_BOOKING_OPTION_HOST,
        array(
            'type'              => 'string',
            'sanitize_callback' => 'esc_url_raw',
            'default'           => '',
        )
    );

    add_settings_section(
        'zambiq_booking_section',
        __('Zambiq Booking', 'zambiq-booking'),
        function () {
            echo '<p>' . esc_html__('Vul de publieke URL van je Zambiq-omgeving in (bijv. https://app.zambiq.nl). Dit is geen geheim.', 'zambiq-booking') . '</p>';
        },
        'zambiq_booking'
    );

    add_settings_field(
        ZAMBIQ_BOOKING_OPTION_HOST,
        __('Widget host URL', 'zambiq-booking'),
        function () {
            $value = get_option(ZAMBIQ_BOOKING_OPTION_HOST, '');
            printf(
                '<input type="url" name="%s" value="%s" class="regular-text" placeholder="https://app.zambiq.nl" />',
                esc_attr(ZAMBIQ_BOOKING_OPTION_HOST),
                esc_attr($value)
            );
        },
        'zambiq_booking',
        'zambiq_booking_section'
    );
}
add_action('admin_init', 'zambiq_booking_register_settings');

/**
 * Add a settings page under the Settings menu.
 */
function zambiq_booking_add_settings_page()
{
    add_options_page(
        __('Zambiq Booking', 'zambiq-booking'),
        __('Zambiq Booking', 'zambiq-booking'),
        'manage_options',
        'zambiq-booking',
        'zambiq_booking_render_settings_page'
    );
}
add_action('admin_menu', 'zambiq_booking_add_settings_page');

function zambiq_booking_render_settings_page()
{
    if (!current_user_can('manage_options')) {
        return;
    }
    ?>
    <div class="wrap">
        <h1><?php echo esc_html__('Zambiq Booking', 'zambiq-booking'); ?></h1>
        <form action="options.php" method="post">
            <?php
            settings_fields('zambiq_booking');
            do_settings_sections('zambiq_booking');
            submit_button();
            ?>
        </form>
        <h2><?php echo esc_html__('Gebruik', 'zambiq-booking'); ?></h2>
        <p><?php echo esc_html__('Plaats deze shortcode op een pagina of bericht:', 'zambiq-booking'); ?></p>
        <p><code>[zambiq_booking restaurant="JE-RESTAURANT-ID" height="720"]</code></p>
    </div>
    <?php
}

/**
 * Shortcode: [zambiq_booking restaurant="GUID" height="720"]
 * Renders the embed container and enqueues widget.js from the configured host.
 */
function zambiq_booking_shortcode($atts)
{
    $atts = shortcode_atts(
        array(
            'restaurant' => '',
            'height'     => '720',
        ),
        $atts,
        'zambiq_booking'
    );

    $host = trim((string) get_option(ZAMBIQ_BOOKING_OPTION_HOST, ''));
    $restaurant = trim((string) $atts['restaurant']);
    $height = (int) $atts['height'];
    if ($height <= 0) {
        $height = 720;
    }

    if ($host === '') {
        return '<p>' . esc_html__('Zambiq: stel eerst de widget host URL in onder Instellingen → Zambiq Booking.', 'zambiq-booking') . '</p>';
    }

    if (!zambiq_booking_is_guid($restaurant)) {
        return '<p>' . esc_html__('Zambiq: geef een geldig restaurant-id op, bijv. [zambiq_booking restaurant="..."].', 'zambiq-booking') . '</p>';
    }

    // Enqueue widget.js once from the configured host (async strategy on WP 6.3+).
    wp_enqueue_script(
        'zambiq-widget',
        trailingslashit($host) . 'widget.js',
        array(),
        '1.0.0',
        array(
            'in_footer' => true,
            'strategy'  => 'async',
        )
    );

    return sprintf(
        '<div data-zambiq-restaurant="%s" data-height="%d"></div>',
        esc_attr($restaurant),
        $height
    );
}
add_shortcode('zambiq_booking', 'zambiq_booking_shortcode');

/**
 * Validate a canonical GUID string.
 */
function zambiq_booking_is_guid($value)
{
    return (bool) preg_match(
        '/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/',
        (string) $value
    );
}
