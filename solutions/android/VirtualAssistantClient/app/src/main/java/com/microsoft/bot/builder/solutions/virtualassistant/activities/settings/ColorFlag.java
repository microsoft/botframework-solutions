package com.microsoft.bot.builder.solutions.virtualassistant.activities.settings;

import android.content.Context;
import android.widget.TextView;

import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.skydoves.colorpickerview.AlphaTileView;
import com.skydoves.colorpickerview.ColorEnvelope;
import com.skydoves.colorpickerview.flag.FlagView;

import butterknife.BindView;
import butterknife.ButterKnife;

public class ColorFlag extends FlagView {

    @BindView(R.id.flag_color_code) TextView textView;
    @BindView(R.id.flag_color_layout) AlphaTileView alphaTileView;

    private Context context;

    /**
     * onBind Views
     * @param context context
     * @param layout custom flagView's layout
     */
    public ColorFlag(Context context, int layout) {
        super(context, layout);
        ButterKnife.bind(this);
        this.context = context;
    }

    /**
     * invoked when selector moved
     * @param colorEnvelope provide color, hexCode, argb
     */
    @Override
    public void onRefresh(ColorEnvelope colorEnvelope) {
        textView.setText("#" + colorEnvelope.getHexCode());
        alphaTileView.setPaintColor(colorEnvelope.getColor());
    }
}
