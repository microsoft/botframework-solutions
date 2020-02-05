package com.microsoft.bot.builder.solutions.virtualassistant.activities.linked_account;

import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.net.Uri;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.View;
import android.webkit.CookieManager;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.ProgressBar;

import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;

import java.net.URISyntaxException;

import butterknife.BindView;
import butterknife.ButterKnife;

public class LinkedAccountActivity extends BaseActivity {

    // VIEWS
    @BindView(R.id.linked_account)
    WebView linkedAccountWebView;
    @BindView(R.id.linked_account_progress_bar)
    ProgressBar progressBar;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_linked_account;
    private static final String SCHEME_MICROSOFT = "microsoft";

    public static Intent getNewIntent(Context context) {
        return new Intent(context, LinkedAccountActivity.class);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);

        WebSettings webSettings = linkedAccountWebView.getSettings();
        webSettings.setJavaScriptEnabled(true);
        webSettings.setJavaScriptCanOpenWindowsAutomatically(true);
        linkedAccountWebView.setOnKeyListener(new View.OnKeyListener() {
            @Override
            public boolean onKey(View v, int keyCode, KeyEvent event) {
                if (event.getAction() == KeyEvent.ACTION_DOWN) {
                    if (keyCode == KeyEvent.KEYCODE_BACK && linkedAccountWebView.canGoBack()) {
                        linkedAccountWebView.goBack();
                        return true;
                    }
                }
                return false;
            }
        });

        CookieManager cookieManager = CookieManager.getInstance();
        cookieManager.setAcceptCookie(true);
        cookieManager.setAcceptThirdPartyCookies(linkedAccountWebView,true);

        linkedAccountWebView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                if (uri.getScheme().equals(SCHEME_MICROSOFT)) {
                    try {
                        Intent intent = Intent.parseUri(uri.toString(), Intent.URI_INTENT_SCHEME);
                        startActivity(intent);
                        finish();
                        return true;
                    } catch (URISyntaxException e) {
                        e.printStackTrace();
                    }
                }
                return super.shouldOverrideUrlLoading(view, request);
            }

            @Override
            public void onPageStarted(WebView view, String url, Bitmap favicon) {
                super.onPageStarted(view, url, favicon);
                showProgressBar(true);
            }

            @Override
            public void onPageFinished(WebView view, String url) {
                super.onPageFinished(view, url);
                showProgressBar(false);
            }
        });

        linkedAccountWebView.loadUrl(configurationManager.getConfiguration().linkedAccountEndpoint + "/Home/LinkedAccounts?companionApp=True&userId=" + configurationManager.getConfiguration().userId);
    }

    private void showProgressBar(boolean show) {
        if (show) {
            progressBar.setVisibility(View.VISIBLE);
        }
        else {
            progressBar.setVisibility(View.GONE);
        }
    }
}
